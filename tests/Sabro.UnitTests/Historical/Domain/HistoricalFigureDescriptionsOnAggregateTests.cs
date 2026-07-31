using Sabro.Historical.Domain;

namespace Sabro.UnitTests.Historical.Domain;

public class HistoricalFigureDescriptionsOnAggregateTests
{
    [Fact]
    public void NewFigure_HasNoDescriptions()
    {
        Figure().Descriptions.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceDescriptions_StoresThemAndTouchesUpdatedAt()
    {
        var figure = Figure();
        var before = figure.UpdatedAt;

        var error = figure.ReplaceDescriptions([Description("en", "A bishop."), Description("fr", "Un évêque.")]);

        error.Should().BeNull();
        figure.Descriptions.Should().HaveCount(2);
        figure.Descriptions.Select(d => d.Language).Should().BeEquivalentTo(["en", "fr"]);
        figure.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ReplaceDescriptions_ReplacesRatherThanAppends()
    {
        var figure = Figure();
        figure.ReplaceDescriptions([Description("en", "First.")]);

        figure.ReplaceDescriptions([Description("fr", "Deuxième.")]);

        figure.Descriptions.Should().ContainSingle();
        figure.Descriptions[0].Language.Should().Be("fr");
    }

    [Fact]
    public void ReplaceDescriptions_WithEmptySet_ClearsThem()
    {
        // The only way to remove a description — there is no separate delete.
        var figure = Figure();
        figure.ReplaceDescriptions([Description("en", "A bishop.")]);

        var error = figure.ReplaceDescriptions([]);

        error.Should().BeNull();
        figure.Descriptions.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceDescriptions_WithNull_ClearsThem()
    {
        var figure = Figure();
        figure.ReplaceDescriptions([Description("en", "A bishop.")]);

        figure.ReplaceDescriptions(null);

        figure.Descriptions.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceDescriptions_WithTwoForTheSameLanguage_IsRejected()
    {
        // A duplicate is not a choice between two texts — the reveal card looks a
        // description up by language, so it would be a coin toss.
        var figure = Figure();

        var error = figure.ReplaceDescriptions([Description("en", "First."), Description("en", "Second.")]);

        error.Should().NotBeNull();
        error!.Message.Should().Contain("one description per language");
        figure.Descriptions.Should().BeEmpty("a rejected replacement must not partially apply");
    }

    [Fact]
    public void Descriptions_DoNotGatePublication()
    {
        // The 289 seeded figures were published before descriptions existed; a
        // publish rule requiring them would have invalidated every one.
        var figure = Figure(HistoricalFigureTradition.WestSyriac);

        var error = figure.Publish();

        error.Should().BeNull();
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
        figure.Descriptions.Should().BeEmpty();
    }

    private static HistoricalFigureDescription Description(string language, string text) =>
        HistoricalFigureDescription.Create(language, text).Value!;

    private static HistoricalFigure Figure(HistoricalFigureTradition? tradition = null) =>
        HistoricalFigure.Create(
            name: "Jacob of Edessa",
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            period: HistoricalPeriod.PostChalcedonian,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: tradition).Value!;
}
