using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Historical.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Localization;
using Sabro.Shared.Results;

namespace Sabro.Historical.Application.Proposals;

/// <summary>
/// Exposes historical figures to the Reviews module as proposal targets, without
/// Reviews having to know anything about Shmo's roster.
/// </summary>
internal sealed class HistoricalFigureProposalTargetSource : IProposalTargetSource
{
    /// <summary>
    /// Attributes of a figure a reviewer may propose a new value for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every attribute here is a <b>game hint</b> in Shmo, which is exactly why this
    /// list matters: 263 of the 289 seeded figures carry unreviewed contested-dating
    /// notes, and an approximate era is a wrong answer rather than a rounding error.
    /// This is the route by which someone who knows the period can correct one
    /// without being handed the whole roster.
    /// </para>
    /// <para>
    /// <c>Status</c> and <c>PlayableInShmo</c> are deliberately absent — publishing a
    /// figure and putting it in the daily pool stay Owner-only.
    /// </para>
    /// </remarks>
    private static readonly string[] FixedProposableFields =
    [
        "name",
        "category",
        "era",
        "period",
        "role",
        "region",
        "tradition",
        "gender",
    ];

    private readonly HistoricalDbContext dbContext;
    private readonly IHistoricalFigureService figures;
    private readonly string[] proposableFields;

    public HistoricalFigureProposalTargetSource(
        HistoricalDbContext dbContext,
        IHistoricalFigureService figures,
        IOptions<SupportedLanguagesOptions> supportedLanguages)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        this.dbContext = dbContext;
        this.figures = figures;
        proposableFields =
        [
            .. FixedProposableFields,
            .. supportedLanguages.Value.Codes.Select(code => $"description.{code}"),
        ];
    }

    public string TargetTypeName => "HistoricalFigure";

    public IReadOnlyCollection<string> ProposableFields => proposableFields;

    public async Task<DateTimeOffset?> GetUpdatedAtAsync(Guid targetId, CancellationToken cancellationToken)
    {
        var updatedAt = await dbContext.Figures
            .AsNoTracking()
            .Where(figure => figure.Id == targetId)
            .Select(figure => (DateTimeOffset?)figure.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return updatedAt;
    }

    public async Task<string?> GetFieldValueAsync(
        Guid targetId,
        string field,
        CancellationToken cancellationToken)
    {
        // No Include for the descriptions: they are an owned collection mapped to the
        // private `descriptions` field, and the configuration `Ignore`s the public
        // property, so it is not a navigation EF can target — including it throws at
        // query-compile time. EF loads owned types with their owner anyway. The same
        // mistake on the Lexicon side made every proposal there fail with a 500; this
        // one had simply never been reached, because nobody had proposed on a figure.
        var figure = await dbContext.Figures
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == targetId, cancellationToken);
        if (figure is null)
        {
            return null;
        }

        if (field.StartsWith("description.", StringComparison.Ordinal))
        {
            var language = field["description.".Length..];
            return figure.Descriptions
                .FirstOrDefault(d => string.Equals(d.Language, language, StringComparison.OrdinalIgnoreCase))
                ?.Text;
        }

        return field switch
        {
            "name" => figure.Name,
            "category" => figure.Category.ToString(),
            "era" => figure.Era.ToString(CultureInfo.InvariantCulture),
            "period" => figure.Period.ToString(),
            "role" => figure.Role.ToString(),
            "region" => figure.Region.ToString(),
            "tradition" => figure.Tradition?.ToString(),
            "gender" => figure.Gender.ToString(),
            _ => null,
        };
    }

    public async Task<IReadOnlyDictionary<Guid, ProposalTargetLabel>> GetLabelsAsync(
        IReadOnlyCollection<Guid> targetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetIds);
        if (targetIds.Count == 0)
        {
            return new Dictionary<Guid, ProposalTargetLabel>();
        }

        var rows = await dbContext.Figures
            .AsNoTracking()
            .Where(figure => targetIds.Contains(figure.Id))
            .Select(figure => new { figure.Id, figure.Name })
            .ToListAsync(cancellationToken);

        // The name alone: it is already Latin script, and it is what the roster is
        // sorted and searched by.
        return rows.ToDictionary(
            row => row.Id,
            row => new ProposalTargetLabel(row.Name));
    }

    public async Task<Error?> ApplyFieldAsync(
        Guid targetId,
        string field,
        string value,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(field);

        var figure = await dbContext.Figures
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == targetId, cancellationToken);
        if (figure is null)
        {
            return Error.NotFound($"HistoricalFigure {targetId}");
        }

        var descriptions = figure.Descriptions
            .Select(description => new HistoricalFigureDescriptionRequest(description.Language, description.Text))
            .ToList();

        if (field.StartsWith("description.", StringComparison.Ordinal))
        {
            var language = field["description.".Length..];
            var existing = descriptions.FindIndex(
                d => string.Equals(d.Language, language, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
            {
                descriptions[existing] = new HistoricalFigureDescriptionRequest(language, value);
            }
            else
            {
                descriptions.Add(new HistoricalFigureDescriptionRequest(language, value));
            }
        }

        // Enum and number fields are free text in a proposal, so they are parsed here
        // with a message that names the field, rather than surfacing a parse failure
        // from somewhere deeper.
        var parsed = ParseScalar(field, value);
        if (parsed.Error is not null)
        {
            return parsed.Error;
        }

        // Rebuilt from what is stored, with the accepted field replaced, then sent
        // through the same service the backoffice form posts to — one write path, with
        // its validation and its publication rules.
        var request = new UpdateHistoricalFigureRequest(
            Name: field == "name" ? value : figure.Name,
            Category: parsed.Category ?? figure.Category,
            Era: parsed.Era ?? figure.Era,
            Period: parsed.Period ?? figure.Period,
            Role: parsed.Role ?? figure.Role,
            Region: parsed.Region ?? figure.Region,
            Gender: parsed.Gender ?? figure.Gender,
            Tradition: field == "tradition" ? parsed.Tradition : figure.Tradition,
            Descriptions: descriptions);

        var result = await figures.UpdateAsync(targetId, request, cancellationToken);
        return result.IsSuccess ? null : result.Error;
    }

    private static ParsedScalar ParseScalar(string field, string value) => field switch
    {
        "category" => Enum.TryParse<HistoricalFigureCategory>(value, out var category)
            ? new ParsedScalar { Category = category }
            : Invalid(field, value),
        "era" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var era)
            ? new ParsedScalar { Era = era }
            : Invalid(field, value),
        "period" => Enum.TryParse<HistoricalPeriod>(value, out var period)
            ? new ParsedScalar { Period = period }
            : Invalid(field, value),
        "role" => Enum.TryParse<HistoricalFigureRole>(value, out var role)
            ? new ParsedScalar { Role = role }
            : Invalid(field, value),
        "region" => Enum.TryParse<HistoricalFigureRegion>(value, out var region)
            ? new ParsedScalar { Region = region }
            : Invalid(field, value),
        "gender" => Enum.TryParse<HistoricalFigureGender>(value, out var gender)
            ? new ParsedScalar { Gender = gender }
            : Invalid(field, value),

        // An empty tradition is a real answer — "not yet decided" — not a parse failure.
        "tradition" => string.IsNullOrWhiteSpace(value)
            ? new ParsedScalar()
            : Enum.TryParse<HistoricalFigureTradition>(value, out var tradition)
                ? new ParsedScalar { Tradition = tradition }
                : Invalid(field, value),
        _ => new ParsedScalar(),
    };

    private static ParsedScalar Invalid(string field, string value) =>
        new() { Error = Error.Validation($"'{value}' is not a valid value for {field}.") };

    private sealed class ParsedScalar
    {
        public Error? Error { get; init; }

        public HistoricalFigureCategory? Category { get; init; }

        public int? Era { get; init; }

        public HistoricalPeriod? Period { get; init; }

        public HistoricalFigureRole? Role { get; init; }

        public HistoricalFigureRegion? Region { get; init; }

        public HistoricalFigureGender? Gender { get; init; }

        public HistoricalFigureTradition? Tradition { get; init; }
    }
}
