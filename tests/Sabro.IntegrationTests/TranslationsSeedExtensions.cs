using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests;

/// <summary>
/// Shared seed helpers for the Translations module's Author / Source /
/// TextVersion / Segment / Annotation chain. Replaces the ~5 copies of
/// SeedSegmentAsync / SeedAnnotationAsync / RandomLetterCode that used to
/// live in individual test classes. Each call seeds a freshly randomized
/// chain so collisions on uniquely-indexed columns (e.g. TextVersion.Code)
/// don't occur across tests sharing the same Postgres container.
/// </summary>
public static class TranslationsSeedExtensions
{
    // Monotonic counter backing NextTextVersionCode. -1 so the first
    // Interlocked.Increment yields 0 ("aaa").
    private static int textVersionCodeSequence = -1;

    /// <summary>
    /// Seeds an Author, Source, TextVersion, and Segment at the given
    /// (chapter, verse). Returns the full id set so call sites can pick
    /// whichever fields they need.
    /// </summary>
    public static async Task<SegmentSeed> SeedSegmentAsync(
        this PostgresFixture postgres,
        int chapter,
        int verse,
        CancellationToken cancellationToken,
        string? content = null)
    {
        var author = Author.Create($"Author-{Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, $"Source-{Guid.NewGuid():N}").Value!;
        var textVersion = TextVersion.Create(
            NextTextVersionCode(),
            $"Tv-{Guid.NewGuid():N}",
            isRightToLeft: false).Value!;
        var segment = Segment.Create(
            source.Id,
            chapter,
            verse,
            textVersion.Id,
            content ?? "Some content.").Value!;

        await using var ctx = postgres.CreateContext();
        ctx.Authors.Add(author);
        ctx.Sources.Add(source);
        ctx.TextVersions.Add(textVersion);
        ctx.Segments.Add(segment);
        await ctx.SaveChangesAsync(cancellationToken);

        return new SegmentSeed(
            SegmentId: segment.Id,
            SourceId: source.Id,
            AuthorId: author.Id,
            TextVersionId: textVersion.Id,
            ChapterNumber: chapter,
            VerseNumber: verse);
    }

    /// <summary>
    /// Seeds an Author, Source, TextVersion, Segment, and Annotation at the
    /// given (chapter, verse). Annotation defaults to anchor [0, 5) and body
    /// "Note body."; pass <paramref name="body"/> to override.
    /// </summary>
    public static async Task<AnnotationSeed> SeedAnnotationAsync(
        this PostgresFixture postgres,
        int chapter,
        int verse,
        CancellationToken cancellationToken,
        string? body = null)
    {
        var author = Author.Create($"Author-{Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, $"Source-{Guid.NewGuid():N}").Value!;
        var textVersion = TextVersion.Create(
            NextTextVersionCode(),
            $"Tv-{Guid.NewGuid():N}",
            isRightToLeft: false).Value!;
        var segment = Segment.Create(
            source.Id,
            chapter,
            verse,
            textVersion.Id,
            "Hello world!").Value!;
        var annotation = Annotation.Create(
            segment.Id,
            anchorStart: 0,
            anchorEnd: 5,
            body ?? "Note body.").Value!;

        await using var ctx = postgres.CreateContext();
        ctx.Authors.Add(author);
        ctx.Sources.Add(source);
        ctx.TextVersions.Add(textVersion);
        ctx.Segments.Add(segment);
        ctx.Annotations.Add(annotation);
        await ctx.SaveChangesAsync(cancellationToken);

        return new AnnotationSeed(
            AnnotationId: annotation.Id,
            AnnotationVersion: annotation.Version,
            SegmentId: segment.Id,
            SourceId: source.Id,
            AuthorId: author.Id,
            TextVersionId: textVersion.Id,
            ChapterNumber: chapter,
            VerseNumber: verse);
    }

    /// <summary>
    /// Returns a process-unique 3-letter lowercase code satisfying the
    /// <c>TextVersion.Code</c> validator (2–3 lowercase letters).
    /// <para>
    /// Drawn from a monotonic counter rather than at random: the previous
    /// random 3-letter generator collided on the unique
    /// <c>ix_text_versions_code</c> index by chance (birthday paradox over only
    /// 26³ values) when many tests seeded into the same Postgres container,
    /// causing intermittent CI failures. A counter guarantees every generated
    /// code is distinct within a test run. Codes are always length 3, so they
    /// also never collide with the 2-letter literal codes used elsewhere.
    /// </para>
    /// All call sites share this single counter so uniqueness holds across test
    /// classes, including those running in parallel (hence Interlocked).
    /// </summary>
    public static string NextTextVersionCode()
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var n = Interlocked.Increment(ref textVersionCodeSequence);
        if (n >= letters.Length * letters.Length * letters.Length)
        {
            throw new InvalidOperationException(
                "Exhausted the 3-letter TextVersion code space for this test run.");
        }

        Span<char> buffer = stackalloc char[3];
        for (var i = buffer.Length - 1; i >= 0; i--)
        {
            buffer[i] = letters[n % letters.Length];
            n /= letters.Length;
        }

        return new string(buffer);
    }
}
