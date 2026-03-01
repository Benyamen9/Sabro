using FluentValidation;
using Sabro.DTOs.Annotations;

namespace Sabro.Validators
{
    public class AnnotationAnchorDtoValidator : AbstractValidator<CreateAnnotationAnchorDto>
    {
        public AnnotationAnchorDtoValidator()
        {
            RuleFor(x => x.SegmentId)
                .GreaterThan(0)
                .WithMessage("Valid segment ID is required");

            // If offsets are provided, validate them
            When(x => x.StartOffset.HasValue, () =>
            {
                RuleFor(x => x.StartOffset)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("StartOffset must be >= 0");

                RuleFor(x => x.EndOffset)
                    .NotNull()
                    .GreaterThan(x => x.StartOffset)
                    .WithMessage("EndOffset must be > StartOffset");

                RuleFor(x => x.AnchorText)
                    .NotEmpty()
                    .WithMessage("AnchorText is required when offsets are provided");
            });
        }
    }
}
