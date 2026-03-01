using FluentValidation;
using Sabro.DTOs.Annotations;
using Sabro.Data.Entities;

namespace Sabro.Validators
{
    public class UpdateAnnotationDtoValidator : AbstractValidator<UpdateAnnotationDto>
    {
        public UpdateAnnotationDtoValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Invalid annotation type");

            RuleFor(x => x.ContentMarkdown)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(50000);

            When(x => x.Type != AnnotationType.Note, () =>
            {
                RuleFor(x => x.AuthorId)
                    .NotNull()
                    .WithMessage("Comment or Citation must have an author");
            });

            RuleFor(x => x.UpdatedAt)
                .NotEmpty()
                .WithMessage("UpdatedAt is required for optimistic locking");
        }
    }
}
