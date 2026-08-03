using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Localization;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Proposals;

/// <summary>
/// Exposes Lexicon entries to the Reviews module as proposal targets, without
/// Reviews having to know anything about the Lexicon.
/// </summary>
internal sealed class LexiconEntryProposalTargetSource : IProposalTargetSource
{
    /// <summary>
    /// Fields of an entry a reviewer may propose a new value for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What is missing here is the point.</b> <c>Status</c> and
    /// <c>PlayableInMeltho</c> are absent, so publishing an entry and putting a word
    /// into Meltho's pool stay Owner-only decisions — a reviewer cannot even ask for
    /// them. That is the business rule "only the Owner publishes entries", enforced
    /// by the list rather than by a check somebody has to remember.
    /// </para>
    /// <para>
    /// <c>rootId</c> and <c>pronunciationAudioUrl</c> are also absent: one is an
    /// opaque foreign key that means nothing as free text, the other is produced by
    /// uploading a file, not by typing a value.
    /// </para>
    /// </remarks>
    private static readonly string[] FixedProposableFields =
    [
        "syriacUnvocalized",
        "syriacVocalized",
        "sblTransliteration",
        "grammaticalCategory",
        "morphology",
    ];

    private readonly LexiconDbContext dbContext;
    private readonly ILexiconEntryService entries;
    private readonly string[] proposableFields;

    public LexiconEntryProposalTargetSource(
        LexiconDbContext dbContext,
        ILexiconEntryService entries,
        IOptions<SupportedLanguagesOptions> supportedLanguages)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        this.dbContext = dbContext;
        this.entries = entries;

        // Meaning fields come from the configured language set rather than a literal
        // list, so adding a language stays the one config change it is everywhere else.
        proposableFields =
        [
            .. FixedProposableFields,
            .. supportedLanguages.Value.Codes.Select(code => $"meaning.{code}"),
        ];
    }

    public string TargetTypeName => "LexiconEntry";

    public IReadOnlyCollection<string> ProposableFields => proposableFields;

    public async Task<DateTimeOffset?> GetUpdatedAtAsync(Guid targetId, CancellationToken cancellationToken)
    {
        // Projected rather than loaded: existence and a timestamp is all this asks
        // for, and materialising the aggregate with its meanings would be wasted work.
        var updatedAt = await dbContext.Entries
            .AsNoTracking()
            .Where(entry => entry.Id == targetId)
            .Select(entry => (DateTimeOffset?)entry.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return updatedAt;
    }

    public async Task<string?> GetFieldValueAsync(
        Guid targetId,
        string field,
        CancellationToken cancellationToken)
    {
        // No Include for the meanings: they are an owned collection mapped to the
        // private `meanings` field, and EF loads owned types with their owner
        // automatically. Including them by the public `Meanings` property throws at
        // query-compile time — the configuration `Ignore`s that property, so it is
        // not a navigation EF can target. Every other read here loads them the same
        // way, by not asking.
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == targetId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        if (field.StartsWith("meaning.", StringComparison.Ordinal))
        {
            var language = field["meaning.".Length..];
            return entry.Meanings
                .FirstOrDefault(m => string.Equals(m.Language, language, StringComparison.OrdinalIgnoreCase))
                ?.Text;
        }

        // Rendered the same way the API serialises them, so a proposal's "before" and
        // the value the reviewer actually saw in the backoffice are the same string.
        return field switch
        {
            "syriacUnvocalized" => entry.SyriacUnvocalized,
            "syriacVocalized" => entry.SyriacVocalized,
            "sblTransliteration" => entry.SblTransliteration,
            "grammaticalCategory" => entry.GrammaticalCategory.ToString(),
            "morphology" => entry.Morphology,
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

        // The unvocalized form identifies the entry, and the transliteration rides
        // along because a queue is scanned, and a Latin handle is faster to scan than
        // an unfamiliar script.
        var rows = await dbContext.Entries
            .AsNoTracking()
            .Where(entry => targetIds.Contains(entry.Id))
            .Select(entry => new { entry.Id, entry.SyriacUnvocalized, entry.SblTransliteration })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.Id,
            row => new ProposalTargetLabel(row.SyriacUnvocalized, row.SblTransliteration));
    }

    public async Task<Error?> ApplyFieldAsync(
        Guid targetId,
        string field,
        string value,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(field);

        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == targetId, cancellationToken);
        if (entry is null)
        {
            return Error.NotFound($"LexiconEntry {targetId}");
        }

        // Rebuilt from what is stored, with the accepted field replaced, and sent
        // through the same service the backoffice form posts to. That keeps NFC
        // normalisation, the Syriac-range validation, the publication rules and the
        // Meilisearch reindex on one path — a proposal must not be a second, quieter
        // way to write content.
        var meanings = entry.Meanings
            .Select(meaning => new CreateLexiconMeaningRequest(meaning.Language, meaning.Text))
            .ToList();

        if (field.StartsWith("meaning.", StringComparison.Ordinal))
        {
            var language = field["meaning.".Length..];
            var existing = meanings.FindIndex(
                m => string.Equals(m.Language, language, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
            {
                meanings[existing] = new CreateLexiconMeaningRequest(language, value);
            }
            else
            {
                meanings.Add(new CreateLexiconMeaningRequest(language, value));
            }
        }
        else if (field == "grammaticalCategory" && !Enum.TryParse<GrammaticalCategory>(value, out _))
        {
            // Caught here rather than in the domain so the message names the field the
            // Owner accepted, not an enum parse failure.
            return Error.Validation($"'{value}' is not a grammatical category.");
        }

        var category = field == "grammaticalCategory"
            ? Enum.Parse<GrammaticalCategory>(value)
            : entry.GrammaticalCategory;

        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: field == "syriacUnvocalized" ? value : entry.SyriacUnvocalized,
            SblTransliteration: field == "sblTransliteration" ? value : entry.SblTransliteration,
            GrammaticalCategory: category,
            SyriacVocalized: field == "syriacVocalized" ? value : entry.SyriacVocalized,
            RootId: entry.RootId,
            TransliterationVariants: entry.TransliterationVariants,
            Morphology: field == "morphology" ? value : entry.Morphology,
            Meanings: meanings);

        var result = await entries.UpdateAsync(targetId, request, cancellationToken);
        return result.IsSuccess ? null : result.Error;
    }
}
