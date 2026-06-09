using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Lexicon.Domain;

public sealed class LexiconEntry : Entity<Guid>, IAggregateRoot
{
    private static readonly string[] RequiredMeaningLanguages = { "en", "fr", "nl" };

    private readonly List<string> transliterationVariants = new();
    private readonly List<LexiconMeaning> meanings = new();

    private LexiconEntry(NormalizedFields fields)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Status = LexiconEntryStatus.Draft;
        PlayableInMeltho = false;
        Apply(fields);
    }

    private LexiconEntry()
    {
    }

    public string SyriacUnvocalized { get; private set; } = string.Empty;

    public string? SyriacVocalized { get; private set; }

    public Guid? RootId { get; private set; }

    public string? SblTransliteration { get; private set; }

    public IReadOnlyList<string> TransliterationVariants => transliterationVariants;

    public GrammaticalCategory GrammaticalCategory { get; private set; }

    public string? Morphology { get; private set; }

    public IReadOnlyList<LexiconMeaning> Meanings => meanings;

    public LexiconEntryStatus Status { get; private set; }

    public bool PlayableInMeltho { get; private set; }

    /// <summary>
    /// Number of base Syriac letters in <see cref="SyriacUnvocalized"/> (combining marks
    /// excluded). Computed on every create/edit, never set directly. Drives Meltho's
    /// 2–8 eligible-pool window.
    /// </summary>
    public int PlayableLength { get; private set; }

    public static Result<LexiconEntry> Create(
        string syriacUnvocalized,
        string? sblTransliteration,
        GrammaticalCategory grammaticalCategory,
        string? syriacVocalized = null,
        Guid? rootId = null,
        IEnumerable<string>? transliterationVariants = null,
        string? morphology = null,
        IEnumerable<LexiconMeaning>? meanings = null)
    {
        var normalized = Normalize(
            syriacUnvocalized,
            sblTransliteration,
            grammaticalCategory,
            syriacVocalized,
            rootId,
            transliterationVariants,
            morphology,
            meanings);
        if (!normalized.IsSuccess)
        {
            return Result<LexiconEntry>.Failure(normalized.Error!);
        }

        return Result<LexiconEntry>.Success(new LexiconEntry(normalized.Value!));
    }

    /// <summary>
    /// Replaces the editable fields (full replace of variants and meanings). Recomputes
    /// <see cref="PlayableLength"/>. Does not change <see cref="Status"/> or
    /// <see cref="PlayableInMeltho"/>. A published entry must keep all required glosses —
    /// an edit that would drop one is rejected; unpublish first.
    /// </summary>
    public Error? Update(
        string syriacUnvocalized,
        string? sblTransliteration,
        GrammaticalCategory grammaticalCategory,
        string? syriacVocalized = null,
        Guid? rootId = null,
        IEnumerable<string>? transliterationVariants = null,
        string? morphology = null,
        IEnumerable<LexiconMeaning>? meanings = null)
    {
        var normalized = Normalize(
            syriacUnvocalized,
            sblTransliteration,
            grammaticalCategory,
            syriacVocalized,
            rootId,
            transliterationVariants,
            morphology,
            meanings);
        if (!normalized.IsSuccess)
        {
            return normalized.Error;
        }

        if (Status == LexiconEntryStatus.Published && !HasAllRequiredMeanings(normalized.Value!.Meanings))
        {
            return Error.Validation(
                "A published entry must keep en, fr, and nl meanings. Unpublish before removing a gloss.");
        }

        Apply(normalized.Value!);
        Touch();
        return null;
    }

    /// <summary>Promotes a draft to published. Requires en/fr/nl meanings. Idempotent when already published.</summary>
    public Error? Publish()
    {
        if (Status == LexiconEntryStatus.Published)
        {
            return null;
        }

        if (!HasAllRequiredMeanings(meanings))
        {
            return Error.Validation("All of en, fr, and nl meanings are required to publish an entry.");
        }

        Status = LexiconEntryStatus.Published;
        Touch();
        return null;
    }

    /// <summary>Returns the entry to draft and clears the playable flag (a draft can never be playable).</summary>
    public void ReturnToDraft()
    {
        if (Status == LexiconEntryStatus.Draft && !PlayableInMeltho)
        {
            return;
        }

        Status = LexiconEntryStatus.Draft;
        PlayableInMeltho = false;
        Touch();
    }

    /// <summary>
    /// Sets the editorial playable flag. Marking playable requires the entry to be published;
    /// the 2–8 length window is enforced by the eligible-pool predicate, not here.
    /// </summary>
    public Error? SetPlayable(bool playable)
    {
        if (playable && Status != LexiconEntryStatus.Published)
        {
            return Error.Conflict("Only published entries can be marked playable.");
        }

        if (PlayableInMeltho == playable)
        {
            return null;
        }

        PlayableInMeltho = playable;
        Touch();
        return null;
    }

    private static Result<NormalizedFields> Normalize(
        string syriacUnvocalized,
        string? sblTransliteration,
        GrammaticalCategory grammaticalCategory,
        string? syriacVocalized,
        Guid? rootId,
        IEnumerable<string>? transliterationVariants,
        string? morphology,
        IEnumerable<LexiconMeaning>? meanings)
    {
        var unvocalizedResult = NormalizeSyriacRequired(syriacUnvocalized, "SyriacUnvocalized");
        if (!unvocalizedResult.IsSuccess)
        {
            return Result<NormalizedFields>.Failure(unvocalizedResult.Error!);
        }

        if (!Enum.IsDefined(grammaticalCategory))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("GrammaticalCategory is not a defined value."));
        }

        string? normalizedVocalized = null;
        if (!string.IsNullOrWhiteSpace(syriacVocalized))
        {
            var vocalizedResult = NormalizeSyriacRequired(syriacVocalized, "SyriacVocalized");
            if (!vocalizedResult.IsSuccess)
            {
                return Result<NormalizedFields>.Failure(vocalizedResult.Error!);
            }

            normalizedVocalized = vocalizedResult.Value;
        }

        if (rootId.HasValue && rootId.Value == Guid.Empty)
        {
            return Result<NormalizedFields>.Failure(Error.Validation("RootId must not be empty."));
        }

        var trimmedSbl = string.IsNullOrWhiteSpace(sblTransliteration) ? null : sblTransliteration.Trim();

        var variants = (transliterationVariants ?? Enumerable.Empty<string>())
            .Select(v => (v ?? string.Empty).Trim())
            .Where(v => v.Length > 0)
            .ToArray();

        var trimmedMorphology = string.IsNullOrWhiteSpace(morphology) ? null : morphology.Trim();

        var meaningList = (meanings ?? Enumerable.Empty<LexiconMeaning>()).ToArray();

        var playableLength = SyriacText.CountLetters(unvocalizedResult.Value!);

        return Result<NormalizedFields>.Success(new NormalizedFields(
            unvocalizedResult.Value!,
            normalizedVocalized,
            rootId,
            trimmedSbl,
            variants,
            grammaticalCategory,
            trimmedMorphology,
            meaningList,
            playableLength));
    }

    private static bool HasAllRequiredMeanings(IEnumerable<LexiconMeaning> meanings)
    {
        var languages = meanings.Select(m => m.Language).ToHashSet(StringComparer.Ordinal);
        return RequiredMeaningLanguages.All(languages.Contains);
    }

    private static Result<string> NormalizeSyriacRequired(string value, string fieldName)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(Error.Validation($"{fieldName} is required."));
        }

        var normalized = SyriacText.Normalize(trimmed);
        if (!SyriacText.IsSyriacOnly(normalized))
        {
            return Result<string>.Failure(Error.Validation($"{fieldName} must contain only Syriac script characters."));
        }

        return Result<string>.Success(normalized);
    }

    private void Apply(NormalizedFields fields)
    {
        SyriacUnvocalized = fields.Unvocalized;
        SyriacVocalized = fields.Vocalized;
        RootId = fields.RootId;
        SblTransliteration = fields.Sbl;
        transliterationVariants.Clear();
        transliterationVariants.AddRange(fields.Variants);
        GrammaticalCategory = fields.Category;
        Morphology = fields.Morphology;
        meanings.Clear();
        meanings.AddRange(fields.Meanings);
        PlayableLength = fields.PlayableLength;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private readonly record struct NormalizedFields(
        string Unvocalized,
        string? Vocalized,
        Guid? RootId,
        string? Sbl,
        IReadOnlyList<string> Variants,
        GrammaticalCategory Category,
        string? Morphology,
        IReadOnlyList<LexiconMeaning> Meanings,
        int PlayableLength);
}
