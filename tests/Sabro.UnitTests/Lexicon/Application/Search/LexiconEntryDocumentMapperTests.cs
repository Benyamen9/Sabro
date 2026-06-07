using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Application.Search;

public class LexiconEntryDocumentMapperTests
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private const string KtbVocalized = "ܟܳܬܶܒ";

    private static readonly string[] Variants = { "kthab", "ktab" };

    private static readonly string[] BilingualMeaningTexts = { "to write", "écrire" };

    private static readonly string[] BilingualMeaningLanguages = { "en", "fr" };

    [Fact]
    public void Map_StringifiesIdAndCategory()
    {
        var entry = NewEntry(syriacVocalized: null, rootId: null, variants: null, meanings: null);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.Id.Should().Be(entry.Id.ToString("D"));
        doc.GrammaticalCategory.Should().Be(nameof(GrammaticalCategory.Verb));
    }

    [Fact]
    public void Map_PassesSyriacAndTransliterationFieldsThrough()
    {
        var entry = NewEntry(syriacVocalized: KtbVocalized, rootId: null, variants: Variants, meanings: null);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        doc.SyriacVocalized.Should().Be(KtbVocalized);
        doc.SblTransliteration.Should().Be("ktb");
        doc.TransliterationVariants.Should().BeEquivalentTo(Variants, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Map_DenormalizesRootIdAndForm()
    {
        var rootId = Guid.NewGuid();
        var entry = NewEntry(syriacVocalized: null, rootId: rootId, variants: null, meanings: null);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: "ܟܬܒ");

        doc.RootId.Should().Be(rootId.ToString("D"));
        doc.RootForm.Should().Be("ܟܬܒ");
    }

    [Fact]
    public void Map_FlattensMeaningsAcrossLanguages()
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "to write").Value!,
            LexiconMeaning.Create("fr", "écrire").Value!,
        };
        var entry = NewEntry(syriacVocalized: null, rootId: null, variants: null, meanings: meanings);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.MeaningTexts.Should().BeEquivalentTo(BilingualMeaningTexts, options => options.WithStrictOrdering());
        doc.MeaningLanguages.Should().BeEquivalentTo(BilingualMeaningLanguages);
    }

    [Fact]
    public void Map_DeduplicatesMeaningLanguages()
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "to write").Value!,
            LexiconMeaning.Create("en", "to inscribe").Value!,
        };
        var entry = NewEntry(syriacVocalized: null, rootId: null, variants: null, meanings: meanings);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.MeaningLanguages.Should().ContainSingle().Which.Should().Be("en");
        doc.MeaningTexts.Should().HaveCount(2);
    }

    [Fact]
    public void Map_EmitsCreatedAtAsUnixSeconds()
    {
        var entry = NewEntry(syriacVocalized: null, rootId: null, variants: null, meanings: null);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.CreatedAtUnix.Should().Be(entry.CreatedAt.ToUnixTimeSeconds());
    }

    [Fact]
    public void Map_EmitsDraftLifecycleStateForNewEntry()
    {
        var entry = NewEntry(syriacVocalized: null, rootId: null, variants: null, meanings: null);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.Status.Should().Be(nameof(LexiconEntryStatus.Draft));
        doc.PlayableInMeltha.Should().BeFalse();
        doc.PlayableLength.Should().Be(3);
    }

    [Fact]
    public void Map_EmitsPublishedAndPlayableState()
    {
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: new[]
            {
                LexiconMeaning.Create("en", "to write").Value!,
                LexiconMeaning.Create("fr", "écrire").Value!,
                LexiconMeaning.Create("nl", "schrijven").Value!,
            }).Value!;
        entry.Publish();
        entry.SetPlayable(true);

        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        doc.Status.Should().Be(nameof(LexiconEntryStatus.Published));
        doc.PlayableInMeltha.Should().BeTrue();
    }

    private static LexiconEntry NewEntry(
        string? syriacVocalized,
        Guid? rootId,
        IEnumerable<string>? variants,
        IEnumerable<LexiconMeaning>? meanings) =>
        LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: syriacVocalized,
            rootId: rootId,
            transliterationVariants: variants,
            morphology: null,
            meanings: meanings).Value!;
}
