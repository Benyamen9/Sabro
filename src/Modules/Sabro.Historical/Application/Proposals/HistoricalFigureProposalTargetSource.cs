using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sabro.Historical.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Localization;

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
    private readonly string[] proposableFields;

    public HistoricalFigureProposalTargetSource(
        HistoricalDbContext dbContext,
        IOptions<SupportedLanguagesOptions> supportedLanguages)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        this.dbContext = dbContext;
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
        var figure = await dbContext.Figures
            .AsNoTracking()
            .Include(f => f.Descriptions)
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
}
