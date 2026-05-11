using FluentValidation;

namespace Sabro.Biblical.Application.Books;

public sealed class CreateBiblicalBookRequestValidator : AbstractValidator<CreateBiblicalBookRequest>
{
    public CreateBiblicalBookRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.EnglishName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.SyriacName)
            .MaximumLength(64)
            .When(x => x.SyriacName is not null);

        RuleFor(x => x.Order).GreaterThanOrEqualTo(1);

        RuleFor(x => x.Testament).IsInEnum();
    }
}
