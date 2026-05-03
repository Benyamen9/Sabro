using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Domain;

public class SegmentTests
{
    private static readonly Guid ASourceId = Guid.NewGuid();
    private static readonly Guid ATextVersionId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsSuccess()
    {
        var result = Segment.Create(
            sourceId: ASourceId,
            chapterNumber: 1,
            verseNumber: 1,
            textVersionId: ATextVersionId,
            content: "In the beginning was the Word.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.SourceId.Should().Be(ASourceId);
        result.Value.ChapterNumber.Should().Be(1);
        result.Value.VerseNumber.Should().Be(1);
        result.Value.TextVersionId.Should().Be(ATextVersionId);
        result.Value.Content.Should().Be("In the beginning was the Word.");
    }

    [Fact]
    public void Create_WithEmptySourceId_ReturnsValidationFailure()
    {
        var result = Segment.Create(Guid.Empty, 1, 1, ATextVersionId, "Content.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyTextVersionId_ReturnsValidationFailure()
    {
        var result = Segment.Create(ASourceId, 1, 1, Guid.Empty, "Content.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveChapterNumber_ReturnsValidationFailure(int chapterNumber)
    {
        var result = Segment.Create(ASourceId, chapterNumber, 1, ATextVersionId, "Content.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveVerseNumber_ReturnsValidationFailure(int verseNumber)
    {
        var result = Segment.Create(ASourceId, 1, verseNumber, ATextVersionId, "Content.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingContent_ReturnsValidationFailure(string? content)
    {
        var result = Segment.Create(ASourceId, 1, 1, ATextVersionId, content!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsContent()
    {
        var result = Segment.Create(ASourceId, 1, 1, ATextVersionId, "   In the beginning.   ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("In the beginning.");
    }

    [Fact]
    public void Create_DefaultsToVersionOneWithNoPredecessor()
    {
        var result = Segment.Create(ASourceId, 1, 1, ATextVersionId, "Content.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(1);
        result.Value.PreviousVersionId.Should().BeNull();
    }

    [Fact]
    public void CreateNextVersion_IncrementsVersionAndLinksToPredecessor()
    {
        var first = Segment.Create(ASourceId, 1, 1, ATextVersionId, "Original.").Value!;

        var next = first.CreateNextVersion("Revised.");

        next.IsSuccess.Should().BeTrue();
        next.Value!.Version.Should().Be(2);
        next.Value.PreviousVersionId.Should().Be(first.Id);
        next.Value.Content.Should().Be("Revised.");
    }

    [Fact]
    public void CreateNextVersion_PreservesLocationFields()
    {
        var first = Segment.Create(ASourceId, 5, 12, ATextVersionId, "Original.").Value!;

        var next = first.CreateNextVersion("Revised.").Value!;

        next.SourceId.Should().Be(ASourceId);
        next.ChapterNumber.Should().Be(5);
        next.VerseNumber.Should().Be(12);
        next.TextVersionId.Should().Be(ATextVersionId);
    }

    [Fact]
    public void CreateNextVersion_AssignsNewId()
    {
        var first = Segment.Create(ASourceId, 1, 1, ATextVersionId, "Original.").Value!;

        var next = first.CreateNextVersion("Revised.").Value!;

        next.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void CreateNextVersion_TrimsNewContent()
    {
        var first = Segment.Create(ASourceId, 1, 1, ATextVersionId, "Original.").Value!;

        var next = first.CreateNextVersion("   Revised.   ").Value!;

        next.Content.Should().Be("Revised.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateNextVersion_WithMissingContent_ReturnsValidationFailure(string? newContent)
    {
        var first = Segment.Create(ASourceId, 1, 1, ATextVersionId, "Original.").Value!;

        var next = first.CreateNextVersion(newContent!);

        next.IsSuccess.Should().BeFalse();
        next.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void CreateNextVersion_AppliedThreeTimes_ProducesIncrementingChain()
    {
        var v1 = Segment.Create(ASourceId, 1, 1, ATextVersionId, "v1").Value!;
        var v2 = v1.CreateNextVersion("v2").Value!;
        var v3 = v2.CreateNextVersion("v3").Value!;

        v1.Version.Should().Be(1);
        v2.Version.Should().Be(2);
        v3.Version.Should().Be(3);
        v2.PreviousVersionId.Should().Be(v1.Id);
        v3.PreviousVersionId.Should().Be(v2.Id);
    }
}
