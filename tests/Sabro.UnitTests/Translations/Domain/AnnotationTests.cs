using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Domain;

public class AnnotationTests
{
    private static readonly Guid ASegmentId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsSuccess()
    {
        var result = Annotation.Create(
            segmentId: ASegmentId,
            anchorStart: 4,
            anchorEnd: 11,
            body: "the Logos");

        result.IsSuccess.Should().BeTrue();
        result.Value!.SegmentId.Should().Be(ASegmentId);
        result.Value.AnchorStart.Should().Be(4);
        result.Value.AnchorEnd.Should().Be(11);
        result.Value.Body.Should().Be("the Logos");
    }

    [Fact]
    public void Create_WithEmptySegmentId_ReturnsValidationFailure()
    {
        var result = Annotation.Create(Guid.Empty, 0, 1, "Body.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeAnchorStart_ReturnsValidationFailure(int anchorStart)
    {
        var result = Annotation.Create(ASegmentId, anchorStart, anchorStart + 5, "Body.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(5, 4)]
    [InlineData(5, 0)]
    public void Create_WithAnchorEndNotAfterStart_ReturnsValidationFailure(int anchorStart, int anchorEnd)
    {
        var result = Annotation.Create(ASegmentId, anchorStart, anchorEnd, "Body.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingBody_ReturnsValidationFailure(string? body)
    {
        var result = Annotation.Create(ASegmentId, 0, 5, body!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBody()
    {
        var result = Annotation.Create(ASegmentId, 0, 5, "   Body.   ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Body.Should().Be("Body.");
    }

    [Fact]
    public void Create_WithZeroAnchorStart_IsAllowed()
    {
        var result = Annotation.Create(ASegmentId, anchorStart: 0, anchorEnd: 1, body: "First char.");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_DefaultsToVersionOneWithNoPredecessor()
    {
        var result = Annotation.Create(ASegmentId, 0, 5, "Body.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(1);
        result.Value.PreviousVersionId.Should().BeNull();
    }

    [Fact]
    public void CreateNextVersion_IncrementsVersionAndLinksToPredecessor()
    {
        var first = Annotation.Create(ASegmentId, 4, 11, "the Logos").Value!;

        var next = first.CreateNextVersion("the divine Word");

        next.IsSuccess.Should().BeTrue();
        next.Value!.Version.Should().Be(2);
        next.Value.PreviousVersionId.Should().Be(first.Id);
        next.Value.Body.Should().Be("the divine Word");
    }

    [Fact]
    public void CreateNextVersion_PreservesAnchorAndSegment()
    {
        var first = Annotation.Create(ASegmentId, 4, 11, "Original.").Value!;

        var next = first.CreateNextVersion("Revised.").Value!;

        next.SegmentId.Should().Be(ASegmentId);
        next.AnchorStart.Should().Be(4);
        next.AnchorEnd.Should().Be(11);
    }

    [Fact]
    public void CreateNextVersion_AssignsNewId()
    {
        var first = Annotation.Create(ASegmentId, 0, 5, "Original.").Value!;

        var next = first.CreateNextVersion("Revised.").Value!;

        next.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void CreateNextVersion_TrimsNewBody()
    {
        var first = Annotation.Create(ASegmentId, 0, 5, "Original.").Value!;

        var next = first.CreateNextVersion("   Revised.   ").Value!;

        next.Body.Should().Be("Revised.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateNextVersion_WithMissingBody_ReturnsValidationFailure(string? newBody)
    {
        var first = Annotation.Create(ASegmentId, 0, 5, "Original.").Value!;

        var next = first.CreateNextVersion(newBody!);

        next.IsSuccess.Should().BeFalse();
        next.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateNextVersion_AppliedThreeTimes_ProducesIncrementingChain()
    {
        var v1 = Annotation.Create(ASegmentId, 0, 5, "v1").Value!;
        var v2 = v1.CreateNextVersion("v2").Value!;
        var v3 = v2.CreateNextVersion("v3").Value!;

        v1.Version.Should().Be(1);
        v2.Version.Should().Be(2);
        v3.Version.Should().Be(3);
        v2.PreviousVersionId.Should().Be(v1.Id);
        v3.PreviousVersionId.Should().Be(v2.Id);
    }
}
