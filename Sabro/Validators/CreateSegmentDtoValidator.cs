using FluentValidation;
using Sabro.DTOs.Segments;

namespace Sabro.Validators
{
    public class CreateSegmentDtoValidator : AbstractValidator<CreateSegmentDto>
    {
        public CreateSegmentDtoValidator()
        {
            RuleFor(x => x.VersionId)
                .GreaterThan(0).WithMessage("VersionId must be a valid ID.");

            RuleFor(x => x.CanonicalRef)
                .NotEmpty().WithMessage("CanonicalRef is required.")
                .MaximumLength(50).WithMessage("CanonicalRef must not exceed 50 characters.");

            RuleFor(x => x.Book)
                .NotEmpty().WithMessage("Book is required.")
                .MaximumLength(100).WithMessage("Book must not exceed 100 characters.");

            RuleFor(x => x.Chapter)
                .NotEmpty().WithMessage("Chapter is required.")
                .MaximumLength(20).WithMessage("Chapter must not exceed 20 characters.");

            RuleFor(x => x.Verse)
                .GreaterThan(0).WithMessage("Verse must be a positive number.");

            RuleFor(x => x.SegmentOrder)
                .GreaterThan(0).WithMessage("SegmentOrder must be a positive number.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .MinimumLength(1).WithMessage("Content must not be empty.");
        }
    }
}
