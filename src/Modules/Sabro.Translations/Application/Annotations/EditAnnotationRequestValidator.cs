using FluentValidation;

namespace Sabro.Translations.Application.Annotations;

public sealed class EditAnnotationRequestValidator : AbstractValidator<EditAnnotationRequest>
{
    public EditAnnotationRequestValidator()
    {
        RuleFor(x => x.AnnotationId).NotEmpty();
        RuleFor(x => x.NewBody).NotEmpty();
    }
}
