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
        => Result<TextVersion>.Success(new TextVersion(code, name, isRightToLeft, isActive));
}
