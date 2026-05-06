using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Lexicon.Domain;

public sealed class LexiconRoot : Entity<Guid>, IAggregateRoot
{
    private LexiconRoot(string form)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Form = form;
    }

    public string Form { get; private set; }

    public static Result<LexiconRoot> Create(string form)
    {
        var trimmed = (form ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<LexiconRoot>.Failure(Error.Validation("Form is required."));
        }

        var normalized = SyriacText.Normalize(trimmed);
        if (!SyriacText.IsSyriacOnly(normalized))
        {
            return Result<LexiconRoot>.Failure(Error.Validation("Form must contain only Syriac script characters."));
        }

        return Result<LexiconRoot>.Success(new LexiconRoot(normalized));
    }
}
