using FluentValidation;
using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

public sealed class CreateApprovalRequestValidator : AbstractValidator<CreateApprovalRequest>
{
    public CreateApprovalRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.SourceId).NotEqual(Guid.Empty);
        RuleFor(x => x.ChapterNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.VerseNumber)
            .NotNull()
            .WithMessage("VerseNumber is required for Segment approvals.")
            .GreaterThanOrEqualTo(1)
            .When(x => x.TargetType == ApprovalTargetType.Segment);

        RuleFor(x => x.VerseNumber)
            .Null()
            .WithMessage("VerseNumber must not be set for Chapter approvals.")
            .When(x => x.TargetType == ApprovalTargetType.Chapter);

        RuleFor(x => x.Version)
            .NotNull()
            .WithMessage("Version is required for Segment approvals.")
            .GreaterThanOrEqualTo(1)
            .When(x => x.TargetType == ApprovalTargetType.Segment);

        RuleFor(x => x.Version)
            .Null()
            .WithMessage("Version must not be set for Chapter approvals.")
            .When(x => x.TargetType == ApprovalTargetType.Chapter);
    }
}
