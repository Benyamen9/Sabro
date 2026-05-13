using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Application.Search;

public class AnnotationDocumentMapperTests
{
    [Fact]
    public void Map_PopulatesAllFieldsFromAnnotationAndSegment()
    {
        var sourceId = Guid.NewGuid();
        var textVersionId = Guid.NewGuid();
        var segment = Segment.Create(sourceId, chapterNumber: 3, verseNumber: 16, textVersionId, "Some content.").Value!;
        var annotation = Annotation.Create(segment.Id, anchorStart: 4, anchorEnd: 12, body: "Footnote body.").Value!;

        var doc = AnnotationDocumentMapper.Map(annotation, segment);

        doc.Id.Should().Be(annotation.Id.ToString("D"));
        doc.SegmentId.Should().Be(segment.Id.ToString("D"));
        doc.SourceId.Should().Be(sourceId.ToString("D"));
        doc.ChapterNumber.Should().Be(3);
        doc.VerseNumber.Should().Be(16);
        doc.AnchorStart.Should().Be(4);
        doc.AnchorEnd.Should().Be(12);
        doc.Body.Should().Be("Footnote body.");
        doc.Version.Should().Be(1);
        doc.ApprovalStatus.Should().BeNull();
    }

    [Fact]
    public void Map_CarriesVersionFromCreateNextVersion()
    {
        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "Some content.").Value!;
        var v1 = Annotation.Create(segment.Id, 0, 5, "v1 body").Value!;
        var v2 = v1.CreateNextVersion("v2 body").Value!;

        var doc = AnnotationDocumentMapper.Map(v2, segment);

        doc.Version.Should().Be(2);
        doc.Body.Should().Be("v2 body");
        doc.Id.Should().Be(v2.Id.ToString("D"));
    }

    [Theory]
    [InlineData(AnnotationApprovalStatus.Approved, "approved")]
    [InlineData(AnnotationApprovalStatus.Rejected, "rejected")]
    public void Map_WithApprovalStatus_LowercasesEnumName(AnnotationApprovalStatus status, string expected)
    {
        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "Content.").Value!;
        var annotation = Annotation.Create(segment.Id, 0, 5, "body").Value!;

        var doc = AnnotationDocumentMapper.Map(annotation, segment, status);

        doc.ApprovalStatus.Should().Be(expected);
    }
}
