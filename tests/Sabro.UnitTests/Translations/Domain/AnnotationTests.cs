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
}
