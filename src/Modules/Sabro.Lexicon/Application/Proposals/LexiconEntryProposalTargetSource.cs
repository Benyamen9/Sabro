using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Localization;

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
    private readonly string[] proposableFields;

    public LexiconEntryProposalTargetSource(
        LexiconDbContext dbContext,
        IOptions<SupportedLanguagesOptions> supportedLanguages)
    {
        ArgumentNullException.ThrowIfNull(supportedLanguages);

        this.dbContext = dbContext;

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
        var entry = await dbContext.Entries
            .AsNoTracking()
            .Include(e => e.Meanings)
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
}
