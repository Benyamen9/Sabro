using FluentValidation;

namespace Sabro.Reviews.Application.SuggestedEdits;

public sealed class CreateFieldProposalRequestValidator : AbstractValidator<CreateFieldProposalRequest>
{
    public CreateFieldProposalRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.TargetId).NotEqual(Guid.Empty);

        // Shape only. Whether the field is one this target actually allows is decided
        // by the owning module's proposable list, not here — that list is the single
        // place which fields exist, and duplicating it would let the two disagree.
        RuleFor(x => x.Field).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ProposedValue).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Rationale).MaximumLength(2000);
    }
}
