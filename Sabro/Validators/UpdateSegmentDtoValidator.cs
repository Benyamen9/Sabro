using FluentValidation;
using Sabro.DTOs.Segments;

namespace Sabro.Validators
{
    public class UpdateSegmentDtoValidator : AbstractValidator<UpdateSegmentDto>
    {
        public UpdateSegmentDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .MinimumLength(1).WithMessage("Content must not be empty.");

            RuleFor(x => x.UpdatedAt)
                .NotEmpty().WithMessage("UpdatedAt is required for concurrency checks.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
                .When(x => x.Reason != null);
        }
    }
}
