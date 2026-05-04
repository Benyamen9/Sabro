using FluentValidation;

namespace Sabro.Translations.Application.Segments;

public sealed class CreateSegmentRequestValidator : AbstractValidator<CreateSegmentRequest>
{
    public CreateSegmentRequestValidator()
    {
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.TextVersionId).NotEmpty();
        RuleFor(x => x.ChapterNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.VerseNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Content).NotEmpty();
    }
}
