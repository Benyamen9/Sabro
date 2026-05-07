using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Application.Search;

public class SegmentDocumentMapperTests
{
    [Fact]
    public void Map_StringifiesAllGuids()
    {
        var sourceId = Guid.NewGuid();
        var textVersionId = Guid.NewGuid();
        var segment = Segment.Create(sourceId, chapterNumber: 1, verseNumber: 1, textVersionId, content: "In the beginning").Value!;

        var doc = SegmentDocumentMapper.Map(segment);

        doc.Id.Should().Be(segment.Id.ToString("D"));
        doc.SourceId.Should().Be(sourceId.ToString("D"));
        doc.TextVersionId.Should().Be(textVersionId.ToString("D"));
    }

    [Fact]
    public void Map_PassesContentAndPositionThrough()
    {
        var segment = Segment.Create(Guid.NewGuid(), chapterNumber: 5, verseNumber: 7, Guid.NewGuid(), content: "Body of text").Value!;

        var doc = SegmentDocumentMapper.Map(segment);

        doc.ChapterNumber.Should().Be(5);
        doc.VerseNumber.Should().Be(7);
        doc.Content.Should().Be("Body of text");
        doc.Version.Should().Be(1);
    }

    [Fact]
    public void Map_OnEditedSegment_CarriesNewVersion()
    {
        var original = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "v1").Value!;
        var next = original.CreateNextVersion("v2").Value!;

        var doc = SegmentDocumentMapper.Map(next);

        doc.Version.Should().Be(2);
        doc.Content.Should().Be("v2");
        doc.Id.Should().Be(next.Id.ToString("D"));
    }
}
