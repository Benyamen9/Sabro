using FluentValidation;

namespace Sabro.Translations.Application.Annotations;

public sealed class CreateAnnotationRequestValidator : AbstractValidator<CreateAnnotationRequest>
{
    public CreateAnnotationRequestValidator()
    {
        RuleFor(x => x.SegmentId).NotEmpty();
        RuleFor(x => x.AnchorStart).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AnchorEnd).GreaterThan(x => x.AnchorStart);
        RuleFor(x => x.Body).NotEmpty();
    }
}
