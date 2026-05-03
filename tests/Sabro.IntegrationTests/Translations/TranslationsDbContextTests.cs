using Microsoft.EntityFrameworkCore;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations;

[Collection(TranslationsCollection.Name)]
public class TranslationsDbContextTests
{
    private readonly PostgresFixture fixture;

    public TranslationsDbContextTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Author_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = Author.Create(
            name: "Dionysios bar Salibi",
            syriacName: "ܕܝܘܢܘܣܝܘܣ",
            title: "Metropolitan of Amid").Value!;

        await using (var write = fixture.CreateContext())
        {
            write.Authors.Add(author);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var loaded = await read.Authors.FirstOrDefaultAsync(a => a.Id == author.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(author.Id);
        loaded.Name.Should().Be(author.Name);
        loaded.SyriacName.Should().Be(author.SyriacName);
        loaded.Title.Should().Be(author.Title);
        loaded.CreatedAt.Should().BeCloseTo(author.CreatedAt, TimeSpan.FromMilliseconds(1));
        loaded.UpdatedAt.Should().BeCloseTo(author.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task TextVersion_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var textVersion = TextVersion.Create("tv", "TestVersion", isRightToLeft: true, isActive: true).Value!;

        await using (var write = fixture.CreateContext())
        {
            write.TextVersions.Add(textVersion);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var loaded = await read.TextVersions.FirstOrDefaultAsync(x => x.Id == textVersion.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.Code.Should().Be("tv");
        loaded.Name.Should().Be("TestVersion");
        loaded.IsRightToLeft.Should().BeTrue();
        loaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Source_RoundTrip_PreservesAllFieldsAndAuthorForeignKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = Author.Create("Some Author").Value!;
        var source = Source.Create(
            authorId: author.Id,
            title: "Commentary on Matthew",
            originalLanguageCode: "syr",
            description: "A description.").Value!;

        await using (var write = fixture.CreateContext())
        {
            write.Authors.Add(author);
            write.Sources.Add(source);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var loaded = await read.Sources.FirstOrDefaultAsync(x => x.Id == source.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.AuthorId.Should().Be(author.Id);
        loaded.Title.Should().Be("Commentary on Matthew");
        loaded.OriginalLanguageCode.Should().Be("syr");
        loaded.Description.Should().Be("A description.");
    }

    [Fact]
    public async Task Segment_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = Author.Create("Author").Value!;
        var source = Source.Create(author.Id, "Some Title").Value!;
        var textVersion = TextVersion.Create("sg", "ForSegment", isRightToLeft: false).Value!;
        var segment = Segment.Create(source.Id, 1, 5, textVersion.Id, "In the beginning.").Value!;

        await using (var write = fixture.CreateContext())
        {
            write.Authors.Add(author);
            write.Sources.Add(source);
            write.TextVersions.Add(textVersion);
            write.Segments.Add(segment);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var loaded = await read.Segments.FirstOrDefaultAsync(x => x.Id == segment.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.SourceId.Should().Be(source.Id);
        loaded.ChapterNumber.Should().Be(1);
        loaded.VerseNumber.Should().Be(5);
        loaded.TextVersionId.Should().Be(textVersion.Id);
        loaded.Content.Should().Be("In the beginning.");
        loaded.Version.Should().Be(1);
        loaded.PreviousVersionId.Should().BeNull();
    }

    [Fact]
    public async Task Segment_NextVersion_PersistsAsTwoRowsLinkedByPreviousVersionId()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = Author.Create("Author").Value!;
        var source = Source.Create(author.Id, "Some Title").Value!;
        var textVersion = TextVersion.Create("sv", "ForSegmentVersioning", isRightToLeft: false).Value!;
        var v1 = Segment.Create(source.Id, 2, 1, textVersion.Id, "v1 content").Value!;
        var v2 = v1.CreateNextVersion("v2 content").Value!;

        await using (var write = fixture.CreateContext())
        {
            write.Authors.Add(author);
            write.Sources.Add(source);
            write.TextVersions.Add(textVersion);
            write.Segments.AddRange(v1, v2);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var rows = await read.Segments
            .Where(s => s.SourceId == source.Id && s.ChapterNumber == 2 && s.VerseNumber == 1)
            .OrderBy(s => s.Version)
            .ToListAsync(ct);

        rows.Should().HaveCount(2);
        rows[0].Version.Should().Be(1);
        rows[0].PreviousVersionId.Should().BeNull();
        rows[1].Version.Should().Be(2);
        rows[1].PreviousVersionId.Should().Be(rows[0].Id);
        rows[1].Content.Should().Be("v2 content");
    }

    [Fact]
    public async Task Annotation_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = Author.Create("Author").Value!;
        var source = Source.Create(author.Id, "Some Title").Value!;
        var textVersion = TextVersion.Create("an", "ForAnnotation", isRightToLeft: false).Value!;
        var segment = Segment.Create(source.Id, 1, 1, textVersion.Id, "Hello world!").Value!;
        var annotation = Annotation.Create(segment.Id, 0, 5, "Greeting word.").Value!;

        await using (var write = fixture.CreateContext())
        {
            write.Authors.Add(author);
            write.Sources.Add(source);
            write.TextVersions.Add(textVersion);
            write.Segments.Add(segment);
            write.Annotations.Add(annotation);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateContext();
        var loaded = await read.Annotations.FirstOrDefaultAsync(x => x.Id == annotation.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.SegmentId.Should().Be(segment.Id);
        loaded.AnchorStart.Should().Be(0);
        loaded.AnchorEnd.Should().Be(5);
        loaded.Body.Should().Be("Greeting word.");
    }
}
