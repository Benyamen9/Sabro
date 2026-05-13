using Sabro.Reviews.Domain;

namespace Sabro.UnitTests.Reviews.Domain;

public class ApprovalTests
{
    private const string DecidedBy = "logto|owner-1";

    private static readonly Guid SourceId = Guid.NewGuid();

    [Fact]
    public void CreateSegment_WithValidInputs_ReturnsApprovedRow()
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 3,
            verseNumber: 7,
            version: 2,
            ApprovalStatus.Approved,
            DecidedBy,
            note: "Looks right.");

        result.IsSuccess.Should().BeTrue();
        var approval = result.Value!;
        approval.TargetType.Should().Be(ApprovalTargetType.Segment);
        approval.SourceId.Should().Be(SourceId);
        approval.ChapterNumber.Should().Be(3);
        approval.VerseNumber.Should().Be(7);
        approval.Version.Should().Be(2);
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.DecisionByLogtoUserId.Should().Be(DecidedBy);
        approval.Note.Should().Be("Looks right.");
        approval.DecisionAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        approval.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateSegment_AcceptsRejectedStatus()
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Rejected,
            DecidedBy);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ApprovalStatus.Rejected);
        result.Value.Note.Should().BeNull();
    }

    [Fact]
    public void CreateChapter_WithValidInputs_ReturnsApprovedRow()
    {
        var result = Approval.CreateChapter(
            SourceId,
            chapterNumber: 5,
            ApprovalStatus.Approved,
            DecidedBy,
            note: "Cascade approval for the whole chapter.");

        result.IsSuccess.Should().BeTrue();
        var approval = result.Value!;
        approval.TargetType.Should().Be(ApprovalTargetType.Chapter);
        approval.SourceId.Should().Be(SourceId);
        approval.ChapterNumber.Should().Be(5);
        approval.VerseNumber.Should().BeNull();
        approval.Version.Should().BeNull();
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.Note.Should().Be("Cascade approval for the whole chapter.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateSegment_WithBlankNote_StoresNull(string? note)
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Approved,
            DecidedBy,
            note: note);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Note.Should().BeNull();
    }

    [Fact]
    public void CreateSegment_TrimsDecisionByAndNote()
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Approved,
            $"  {DecidedBy}  ",
            note: "  Whitespace trimmed.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.DecisionByLogtoUserId.Should().Be(DecidedBy);
        result.Value.Note.Should().Be("Whitespace trimmed.");
    }

    [Fact]
    public void CreateSegment_WithEmptySourceId_ReturnsValidationError()
    {
        var result = Approval.CreateSegment(
            Guid.Empty,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSegment_WithNonPositiveChapterNumber_ReturnsValidationError(int chapterNumber)
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSegment_WithNonPositiveVerseNumber_ReturnsValidationError(int verseNumber)
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber,
            version: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSegment_WithNonPositiveVersion_ReturnsValidationError(int version)
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateSegment_WithBlankDecidedBy_ReturnsValidationError(string? decidedBy)
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            ApprovalStatus.Approved,
            decidedBy!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateSegment_WithUndefinedStatus_ReturnsValidationError()
    {
        var result = Approval.CreateSegment(
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            version: 1,
            (ApprovalStatus)999,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateChapter_WithEmptySourceId_ReturnsValidationError()
    {
        var result = Approval.CreateChapter(
            Guid.Empty,
            chapterNumber: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateChapter_WithNonPositiveChapterNumber_ReturnsValidationError(int chapterNumber)
    {
        var result = Approval.CreateChapter(
            SourceId,
            chapterNumber,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateChapter_WithBlankDecidedBy_ReturnsValidationError(string? decidedBy)
    {
        var result = Approval.CreateChapter(
            SourceId,
            chapterNumber: 1,
            ApprovalStatus.Approved,
            decidedBy!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateAnnotation_WithValidInputs_DenormalizesParentLocator()
    {
        var annotationId = Guid.NewGuid();

        var result = Approval.CreateAnnotation(
            annotationId,
            version: 2,
            SourceId,
            chapterNumber: 3,
            verseNumber: 7,
            ApprovalStatus.Approved,
            DecidedBy,
            note: "Footnote checks out.");

        result.IsSuccess.Should().BeTrue();
        var approval = result.Value!;
        approval.TargetType.Should().Be(ApprovalTargetType.Annotation);
        approval.AnnotationId.Should().Be(annotationId);
        approval.Version.Should().Be(2);
        approval.SourceId.Should().Be(SourceId);
        approval.ChapterNumber.Should().Be(3);
        approval.VerseNumber.Should().Be(7);
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.Note.Should().Be("Footnote checks out.");
    }

    [Fact]
    public void CreateAnnotation_LeavesAnnotationIdNullOnSegmentAndChapter()
    {
        var segment = Approval.CreateSegment(SourceId, 1, 1, 1, ApprovalStatus.Approved, DecidedBy).Value!;
        var chapter = Approval.CreateChapter(SourceId, 1, ApprovalStatus.Approved, DecidedBy).Value!;

        segment.AnnotationId.Should().BeNull();
        chapter.AnnotationId.Should().BeNull();
    }

    [Fact]
    public void CreateAnnotation_WithEmptyAnnotationId_ReturnsValidationError()
    {
        var result = Approval.CreateAnnotation(
            Guid.Empty,
            version: 1,
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateAnnotation_WithNonPositiveVersion_ReturnsValidationError(int version)
    {
        var result = Approval.CreateAnnotation(
            Guid.NewGuid(),
            version,
            SourceId,
            chapterNumber: 1,
            verseNumber: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateAnnotation_WithNonPositiveVerseNumber_ReturnsValidationError(int verseNumber)
    {
        var result = Approval.CreateAnnotation(
            Guid.NewGuid(),
            version: 1,
            SourceId,
            chapterNumber: 1,
            verseNumber,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateAnnotation_WithEmptySourceId_ReturnsValidationError()
    {
        var result = Approval.CreateAnnotation(
            Guid.NewGuid(),
            version: 1,
            Guid.Empty,
            chapterNumber: 1,
            verseNumber: 1,
            ApprovalStatus.Approved,
            DecidedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
