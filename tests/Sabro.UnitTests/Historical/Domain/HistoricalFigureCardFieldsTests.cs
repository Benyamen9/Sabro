using System.Text;
using Sabro.Historical.Domain;
using Sabro.Shared.Results;

namespace Sabro.UnitTests.Historical.Domain;

/// <summary>
/// The fields that dress a figure's card in the hub's collection — emblem, Syriac
/// name, read-more link. All optional, none of them a Shmo hint.
/// </summary>
public class HistoricalFigureCardFieldsTests
{
    private const string Ephrem = "ܡܪܝ ܐܦܪܝܡ";

    [Fact]
    public void Create_WithoutCardFields_LeavesThemEmpty()
    {
        var figure = NewDraft().Value!;

        figure.Emblem.Should().BeNull();
        figure.SyriacName.Should().BeNull();
        figure.ReadMoreUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithCardFields_StoresThem()
    {
        var result = NewDraft(
            emblem: HistoricalFigureEmblem.Harp,
            syriacName: Ephrem,
            readMoreUrl: "https://syriaca.org/person/13");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Emblem.Should().Be(HistoricalFigureEmblem.Harp);
        result.Value.SyriacName.Should().Be(Ephrem);
        result.Value.ReadMoreUrl.Should().Be("https://syriaca.org/person/13");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCardFields_StoresNull(string blank)
    {
        var figure = NewDraft(syriacName: blank, readMoreUrl: blank).Value!;

        figure.SyriacName.Should().BeNull();
        figure.ReadMoreUrl.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsSyriacNameAndStoresItNfc()
    {
        var figure = NewDraft(syriacName: $"  {Ephrem}  ").Value!;

        figure.SyriacName.Should().Be(Ephrem);
        figure.SyriacName!.IsNormalized(NormalizationForm.FormC).Should().BeTrue();
    }

    [Theory]
    [InlineData("Mor Aphrem")]
    [InlineData("ܡܪܝ Aphrem")]
    public void Create_WithNonSyriacName_ReturnsValidationFailure(string syriacName)
    {
        var result = NewDraft(syriacName: syriacName);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithOverlongSyriacName_ReturnsValidationFailure()
    {
        var result = NewDraft(syriacName: new string('ܐ', HistoricalFigure.MaxSyriacNameLength + 1));

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("syriaca.org/person/13")]
    [InlineData("/person/13")]
    [InlineData("ftp://syriaca.org/person/13")]
    [InlineData("javascript:alert(1)")]
    public void Create_WithInvalidReadMoreUrl_ReturnsValidationFailure(string url)
    {
        var result = NewDraft(readMoreUrl: url);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedEmblem_ReturnsValidationFailure()
    {
        var result = NewDraft(emblem: (HistoricalFigureEmblem)9999);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Update_WithoutCardFields_ClearsThem()
    {
        // Update is a full replacement, like the descriptions: what is not sent is gone.
        var figure = NewDraft(
            emblem: HistoricalFigureEmblem.Keys,
            syriacName: Ephrem,
            readMoreUrl: "https://syriaca.org/person/13").Value!;

        var error = figure.Update(
            "Simon Peter",
            HistoricalFigureCategory.BiblicalNewTestament,
            1,
            HistoricalPeriod.Apostolic,
            HistoricalFigureRole.Apostle,
            HistoricalFigureRegion.IsraelJudah,
            HistoricalFigureGender.Male);

        error.Should().BeNull();
        figure.Emblem.Should().BeNull();
        figure.SyriacName.Should().BeNull();
        figure.ReadMoreUrl.Should().BeNull();
    }

    [Fact]
    public void Update_WithInvalidReadMoreUrl_KeepsPreviousValues()
    {
        var figure = NewDraft(emblem: HistoricalFigureEmblem.Keys).Value!;

        var error = figure.Update(
            "Simon Peter",
            HistoricalFigureCategory.BiblicalNewTestament,
            1,
            HistoricalPeriod.Apostolic,
            HistoricalFigureRole.Apostle,
            HistoricalFigureRegion.IsraelJudah,
            HistoricalFigureGender.Male,
            emblem: HistoricalFigureEmblem.Ship,
            readMoreUrl: "not a link");

        error.Should().NotBeNull();
        figure.Emblem.Should().Be(HistoricalFigureEmblem.Keys);
    }

    private static Result<HistoricalFigure> NewDraft(
        HistoricalFigureEmblem? emblem = null,
        string? syriacName = null,
        string? readMoreUrl = null) =>
        HistoricalFigure.Create(
            name: "Ephrem the Syrian",
            category: HistoricalFigureCategory.Patristic,
            era: 4,
            period: HistoricalPeriod.NiceneEra,
            role: HistoricalFigureRole.Commentator,
            region: HistoricalFigureRegion.Mesopotamia,
            gender: HistoricalFigureGender.Male,
            emblem: emblem,
            syriacName: syriacName,
            readMoreUrl: readMoreUrl);
}
