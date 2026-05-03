using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Translations.Domain;

public sealed class TextVersion : Entity<Guid>, IAggregateRoot
{
    private TextVersion(string code, string name, bool isRightToLeft, bool isActive)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Code = code;
        Name = name;
        IsRightToLeft = isRightToLeft;
        IsActive = isActive;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsRightToLeft { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<TextVersion> Create(string code, string name, bool isRightToLeft, bool isActive = false)
    {
        var normalizedCode = (code ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedName = (name ?? string.Empty).Trim();

        if (normalizedCode.Length == 0)
        {
            return Result<TextVersion>.Failure(Error.Validation("Code is required."));
        }

        if (!IsValidCodeFormat(normalizedCode))
        {
            return Result<TextVersion>.Failure(Error.Validation("Code must be 2 or 3 lowercase letters."));
        }

        if (normalizedName.Length == 0)
        {
            return Result<TextVersion>.Failure(Error.Validation("Name is required."));
        }

        return Result<TextVersion>.Success(new TextVersion(normalizedCode, normalizedName, isRightToLeft, isActive));
    }

    private static bool IsValidCodeFormat(string code)
    {
        if (code.Length is < 2 or > 3)
        {
            return false;
        }

        foreach (var c in code)
        {
            if (c is < 'a' or > 'z')
            {
                return false;
            }
        }

        return true;
    }
}
