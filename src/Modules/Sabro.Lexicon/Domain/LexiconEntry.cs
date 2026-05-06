using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Lexicon.Domain;

public sealed class LexiconEntry : Entity<Guid>, IAggregateRoot
{
    private readonly List<string> transliterationVariants = new();
    private readonly List<LexiconMeaning> meanings = new();

    private LexiconEntry(
        string syriacUnvocalized,
        string? syriacVocalized,
        Guid? rootId,
        string sblTransliteration,
        IEnumerable<string> transliterationVariants,
        GrammaticalCategory grammaticalCategory,
        string? morphology,
        IEnumerable<LexiconMeaning> meanings)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        SyriacUnvocalized = syriacUnvocalized;
        SyriacVocalized = syriacVocalized;
        RootId = rootId;
        SblTransliteration = sblTransliteration;
        this.transliterationVariants.AddRange(transliterationVariants);
        GrammaticalCategory = grammaticalCategory;
        Morphology = morphology;
        this.meanings.AddRange(meanings);
    }

    private LexiconEntry()
    {
        SyriacUnvocalized = string.Empty;
        SblTransliteration = string.Empty;
    }

    public string SyriacUnvocalized { get; private set; }

    public string? SyriacVocalized { get; private set; }

    public Guid? RootId { get; private set; }

    public string SblTransliteration { get; private set; }

    public IReadOnlyList<string> TransliterationVariants => transliterationVariants;

    public GrammaticalCategory GrammaticalCategory { get; private set; }

    public string? Morphology { get; private set; }

    public IReadOnlyList<LexiconMeaning> Meanings => meanings;

    public static Result<LexiconEntry> Create(
        string syriacUnvocalized,
        string sblTransliteration,
        GrammaticalCategory grammaticalCategory,
        string? syriacVocalized = null,
        Guid? rootId = null,
        IEnumerable<string>? transliterationVariants = null,
        string? morphology = null,
        IEnumerable<LexiconMeaning>? meanings = null)
    {
        var unvocalizedResult = NormalizeSyriacRequired(syriacUnvocalized, "SyriacUnvocalized");
        if (!unvocalizedResult.IsSuccess)
        {
            return Result<LexiconEntry>.Failure(unvocalizedResult.Error!);
        }

        var trimmedSbl = (sblTransliteration ?? string.Empty).Trim();
        if (trimmedSbl.Length == 0)
        {
            return Result<LexiconEntry>.Failure(Error.Validation("SblTransliteration is required."));
        }

        if (!Enum.IsDefined(grammaticalCategory))
        {
            return Result<LexiconEntry>.Failure(Error.Validation("GrammaticalCategory is not a defined value."));
        }

        string? normalizedVocalized = null;
        if (!string.IsNullOrWhiteSpace(syriacVocalized))
        {
            var vocalizedResult = NormalizeSyriacRequired(syriacVocalized, "SyriacVocalized");
            if (!vocalizedResult.IsSuccess)
            {
                return Result<LexiconEntry>.Failure(vocalizedResult.Error!);
            }

            normalizedVocalized = vocalizedResult.Value;
        }

        if (rootId.HasValue && rootId.Value == Guid.Empty)
        {
            return Result<LexiconEntry>.Failure(Error.Validation("RootId must not be empty."));
        }

        var variants = (transliterationVariants ?? Enumerable.Empty<string>())
            .Select(v => (v ?? string.Empty).Trim())
            .Where(v => v.Length > 0)
            .ToArray();

        var trimmedMorphology = string.IsNullOrWhiteSpace(morphology) ? null : morphology.Trim();

        var meaningList = (meanings ?? Enumerable.Empty<LexiconMeaning>()).ToArray();

        return Result<LexiconEntry>.Success(new LexiconEntry(
            unvocalizedResult.Value!,
            normalizedVocalized,
            rootId,
            trimmedSbl,
            variants,
            grammaticalCategory,
            trimmedMorphology,
            meaningList));
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
}
