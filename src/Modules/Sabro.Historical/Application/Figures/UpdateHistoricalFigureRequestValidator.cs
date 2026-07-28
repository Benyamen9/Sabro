using FluentValidation;

namespace Sabro.Historical.Application.Figures;

public sealed class UpdateHistoricalFigureRequestValidator : AbstractValidator<UpdateHistoricalFigureRequest>
{
    public UpdateHistoricalFigureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Region).IsInEnum();
        RuleFor(x => x.Gender).IsInEnum();

        RuleFor(x => x.Tradition!.Value)
            .IsInEnum()
            .When(x => x.Tradition.HasValue);

        RuleFor(x => x.Era)
            .NotEqual(0)
            .WithMessage("Era must not be zero — there is no century zero.");
    }
}
