using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Biblical.Domain;

public sealed class BiblicalBook : Entity<Guid>, IAggregateRoot
{
    private BiblicalBook(string code, string englishName, string? syriacName, Testament testament, int order)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Code = code;
        EnglishName = englishName;
        SyriacName = syriacName;
        Testament = testament;
        Order = order;
    }

    /// <summary>
    /// Short canonical code in uppercase, e.g. <c>MAT</c>, <c>JHN</c>, <c>GEN</c>.
    /// Unique across the catalog. Stable identifier used in API queries so callers
    /// don't have to know the row id.
    /// </summary>
    public string Code { get; private set; }

    public string EnglishName { get; private set; }

    public string? SyriacName { get; private set; }

    public Testament Testament { get; private set; }

    /// <summary>
    /// Canonical position used for sorting (e.g. Genesis = 1, Matthew = 40).
    /// Not constrained — different traditions order the canon differently and
    /// we don't want to bake a single ordering into the schema.
    /// </summary>
    public int Order { get; private set; }

    public static Result<BiblicalBook> Create(
        string code,
        string englishName,
        Testament testament,
        int order,
        string? syriacName = null)
    {
        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCode.Length == 0)
        {
            return Result<BiblicalBook>.Failure(Error.Validation("Code is required."));
        }

        if (!IsValidCodeFormat(normalizedCode))
        {
            return Result<BiblicalBook>.Failure(Error.Validation("Code must be 2 to 8 uppercase letters or digits."));
        }

        var trimmedEnglishName = (englishName ?? string.Empty).Trim();
        if (trimmedEnglishName.Length == 0)
        {
            return Result<BiblicalBook>.Failure(Error.Validation("EnglishName is required."));
        }

        var trimmedSyriacName = string.IsNullOrWhiteSpace(syriacName) ? null : syriacName.Trim();
        if (trimmedSyriacName is not null)
        {
            var normalized = SyriacText.Normalize(trimmedSyriacName);
            if (!SyriacText.IsSyriacOnly(normalized))
            {
                return Result<BiblicalBook>.Failure(Error.Validation("SyriacName must contain only Syriac script characters."));
            }

            trimmedSyriacName = normalized;
        }

        if (order < 1)
        {
            return Result<BiblicalBook>.Failure(Error.Validation("Order must be 1 or greater."));
        }

        if (!Enum.IsDefined(testament))
        {
            return Result<BiblicalBook>.Failure(Error.Validation("Testament is invalid."));
        }

        return Result<BiblicalBook>.Success(
            new BiblicalBook(normalizedCode, trimmedEnglishName, trimmedSyriacName, testament, order));
    }

    private static bool IsValidCodeFormat(string code)
    {
        if (code.Length is < 2 or > 8)
        {
            return false;
        }

        foreach (var c in code)
        {
            if (!(c is >= 'A' and <= 'Z' or >= '0' and <= '9'))
            {
                return false;
            }
        }

        return true;
    }
}
