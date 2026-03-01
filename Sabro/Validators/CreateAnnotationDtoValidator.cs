using Sabro.DTOs.Annotations;
using FluentValidation;
using Sabro.Data.Entities;

namespace Sabro.Validators
{
    public class CreateAnnotationDtoValidator : AbstractValidator<CreateAnnotationDto>
    {
        public CreateAnnotationDtoValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid annotation type");

            RuleFor(x => x.ContentMarkdown)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(50000)
                .WithMessage("Content must be between 1 and 50000 characters");

            // Comment/Citation must have an author
            When(x => x.Type != AnnotationType.Note, () =>
            {
                RuleFor(x => x.AuthorId)
                    .NotNull()
                    .WithMessage("Comment or Citation must have an author");
            });

            // At least one anchor required
            RuleFor(x => x.Anchors)
                .NotEmpty()
                .WithMessage("At least one anchor is required");

            // Validate each anchor
            RuleForEach(x => x.Anchors)
                .SetValidator(new AnnotationAnchorDtoValidator());
        }
    }
}
