using FluentValidation;

namespace Sabro.Translations.Application.Segments;

public sealed class EditSegmentRequestValidator : AbstractValidator<EditSegmentRequest>
{
    public EditSegmentRequestValidator()
    {
        RuleFor(x => x.SegmentId).NotEmpty();
        RuleFor(x => x.NewContent).NotEmpty();
    }
}
