using FluentValidation;

namespace Sabro.Biblical.Application.Passages;

public sealed class GetOrCreateBiblicalPassageRequestValidator : AbstractValidator<GetOrCreateBiblicalPassageRequest>
{
    public GetOrCreateBiblicalPassageRequestValidator()
    {
        RuleFor(x => x.BookCode)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.ChapterNumber).GreaterThanOrEqualTo(1);

        RuleFor(x => x.VerseNumber).GreaterThanOrEqualTo(1);
    }
}
