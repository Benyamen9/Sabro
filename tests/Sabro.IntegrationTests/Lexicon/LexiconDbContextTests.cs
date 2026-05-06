using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Lexicon;

[Collection(TranslationsCollection.Name)]
public class LexiconDbContextTests
{
    private const string KtbUnvocalized = "ܟܬܒ";
    private const string KtbVocalized = "ܟܬ݂ܳܒ݂";

    private static readonly string[] TwoVariants = { "kthab", "ktab" };

    private static readonly string[] ThreeLanguages = { "en", "fr", "nl" };

    private static readonly string[] ThreeMeaningTexts = { "to write", "écrire", "schrijven" };

    private readonly PostgresFixture fixture;

    public LexiconDbContextTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task LexiconRoot_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var root = LexiconRoot.Create(RandomSyriacRoot()).Value!;

        await using (var write = fixture.CreateLexiconContext())
        {
            write.Roots.Add(root);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Roots.FirstOrDefaultAsync(r => r.Id == root.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.Form.Should().Be(root.Form);
        loaded.CreatedAt.Should().BeCloseTo(root.CreatedAt, TimeSpan.FromMilliseconds(1));
        loaded.UpdatedAt.Should().BeCloseTo(root.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task LexiconEntry_RoundTrip_PreservesAllScalarFieldsAndVariants()
    {
        var ct = TestContext.Current.CancellationToken;
        var root = LexiconRoot.Create(RandomSyriacRoot()).Value!;
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: KtbVocalized,
            rootId: root.Id,
            transliterationVariants: TwoVariants,
            morphology: "Pe'al, transitive").Value!;

        await using (var write = fixture.CreateLexiconContext())
        {
            write.Roots.Add(root);
            write.Entries.Add(entry);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == entry.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        loaded.SyriacVocalized.Should().Be(KtbVocalized);
        loaded.RootId.Should().Be(root.Id);
        loaded.SblTransliteration.Should().Be("ktb");
        loaded.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        loaded.Morphology.Should().Be("Pe'al, transitive");
        loaded.TransliterationVariants.Should().BeEquivalentTo(
            TwoVariants,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task LexiconEntry_RoundTrip_PreservesMeaningsInOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "to write").Value!,
            LexiconMeaning.Create("fr", "écrire").Value!,
            LexiconMeaning.Create("nl", "schrijven").Value!,
        };
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: meanings).Value!;

        await using (var write = fixture.CreateLexiconContext())
        {
            write.Entries.Add(entry);
            await write.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == entry.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.Meanings.Should().HaveCount(3);
        loaded.Meanings.Select(m => m.Language).Should().BeEquivalentTo(
            ThreeLanguages,
            options => options.WithStrictOrdering());
        loaded.Meanings.Select(m => m.Text).Should().BeEquivalentTo(
            ThreeMeaningTexts,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task LexiconEntry_DeletingRoot_NullifiesEntryRootId()
    {
        var ct = TestContext.Current.CancellationToken;
        var root = LexiconRoot.Create(RandomSyriacRoot()).Value!;
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            rootId: root.Id).Value!;

        await using (var write = fixture.CreateLexiconContext())
        {
            write.Roots.Add(root);
            write.Entries.Add(entry);
            await write.SaveChangesAsync(ct);
        }

        await using (var deleter = fixture.CreateLexiconContext())
        {
            var tracked = await deleter.Roots.FirstAsync(r => r.Id == root.Id, ct);
            deleter.Roots.Remove(tracked);
            await deleter.SaveChangesAsync(ct);
        }

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == entry.Id, ct);
        loaded.Should().NotBeNull();
        loaded!.RootId.Should().BeNull();
    }

    private static string RandomSyriacRoot()
    {
        // Random 5-letter sequence from the Syriac base consonants (U+0710..U+0725)
        // — uniqueness guard for the unique index on lexicon_roots.form across test runs.
        return new string(new[]
        {
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
        });
    }
}
