using FluentValidation;
using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

public sealed class CreateApprovalRequestValidator : AbstractValidator<CreateApprovalRequest>
{
    public CreateApprovalRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();

        // Segment: source + chapter + verse + version required; annotation forbidden.
        When(x => x.TargetType == ApprovalTargetType.Segment, () =>
        {
            RuleFor(x => x.SourceId)
                .NotNull().NotEqual(Guid.Empty)
                .WithMessage("SourceId is required for Segment approvals.");
            RuleFor(x => x.ChapterNumber)
                .NotNull().GreaterThanOrEqualTo(1)
                .WithMessage("ChapterNumber is required for Segment approvals.");
            RuleFor(x => x.VerseNumber)
                .NotNull().GreaterThanOrEqualTo(1)
                .WithMessage("VerseNumber is required for Segment approvals.");
            RuleFor(x => x.Version)
                .NotNull().GreaterThanOrEqualTo(1)
                .WithMessage("Version is required for Segment approvals.");
            RuleFor(x => x.AnnotationId)
                .Null()
                .WithMessage("AnnotationId must not be set for Segment approvals.");
        });

        // Chapter: source + chapter required; verse, version, annotation forbidden.
        When(x => x.TargetType == ApprovalTargetType.Chapter, () =>
        {
            RuleFor(x => x.SourceId)
                .NotNull().NotEqual(Guid.Empty)
                .WithMessage("SourceId is required for Chapter approvals.");
            RuleFor(x => x.ChapterNumber)
                .NotNull().GreaterThanOrEqualTo(1)
                .WithMessage("ChapterNumber is required for Chapter approvals.");
            RuleFor(x => x.VerseNumber)
                .Null()
                .WithMessage("VerseNumber must not be set for Chapter approvals.");
            RuleFor(x => x.Version)
                .Null()
                .WithMessage("Version must not be set for Chapter approvals.");
            RuleFor(x => x.AnnotationId)
                .Null()
                .WithMessage("AnnotationId must not be set for Chapter approvals.");
        });

        // Annotation: only AnnotationId required; service resolves the locator from the parent Segment.
        When(x => x.TargetType == ApprovalTargetType.Annotation, () =>
        {
            RuleFor(x => x.AnnotationId)
                .NotNull().NotEqual(Guid.Empty)
                .WithMessage("AnnotationId is required for Annotation approvals.");
            RuleFor(x => x.SourceId)
                .Null()
                .WithMessage("SourceId must not be set for Annotation approvals; the parent locator is resolved server-side.");
            RuleFor(x => x.ChapterNumber)
                .Null()
                .WithMessage("ChapterNumber must not be set for Annotation approvals; the parent locator is resolved server-side.");
            RuleFor(x => x.VerseNumber)
                .Null()
                .WithMessage("VerseNumber must not be set for Annotation approvals; the parent locator is resolved server-side.");
            RuleFor(x => x.Version)
                .Null()
                .WithMessage("Version must not be set for Annotation approvals; the parent locator is resolved server-side.");
        });
    }
}
