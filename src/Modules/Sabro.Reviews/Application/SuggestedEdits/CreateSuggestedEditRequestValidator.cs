using FluentValidation;

namespace Sabro.Reviews.Application.SuggestedEdits;

public sealed class CreateSuggestedEditRequestValidator : AbstractValidator<CreateSuggestedEditRequest>
{
    public CreateSuggestedEditRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.TargetId).NotEqual(Guid.Empty);
        RuleFor(x => x.TargetVersion).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ProposedContent).NotEmpty();
    }
}
