using Sabro.Historical.Domain;
using Sabro.Shared.Results;

namespace Sabro.UnitTests.Historical.Domain;

public class HistoricalFigureTests
{
    private const string JacobOfEdessa = "Jacob of Edessa";

    [Fact]
    public void Create_WithMinimalValidInput_ReturnsDraft()
    {
        var result = HistoricalFigure.Create(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(JacobOfEdessa);
        result.Value.Category.Should().Be(HistoricalFigureCategory.Patristic);
        result.Value.Era.Should().Be(7);
        result.Value.Role.Should().Be(HistoricalFigureRole.Bishop);
        result.Value.Region.Should().Be(HistoricalFigureRegion.Syria);
        result.Value.Gender.Should().Be(HistoricalFigureGender.Male);
        result.Value.Tradition.Should().BeNull();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.Status.Should().Be(HistoricalFigureStatus.Draft);
        result.Value.PlayableInShmo.Should().BeFalse();
    }

    [Fact]
    public void Create_WithTradition_StoresIt()
    {
        var result = HistoricalFigure.Create(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: HistoricalFigureTradition.WestSyriac);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tradition.Should().Be(HistoricalFigureTradition.WestSyriac);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingName_ReturnsValidationFailure(string? name)
    {
        var result = NewDraft(name: name!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_TrimsName()
    {
        var result = NewDraft(name: $"   {JacobOfEdessa}   ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(JacobOfEdessa);
    }

    [Fact]
    public void Create_WithOverlongName_ReturnsValidationFailure()
    {
        var result = NewDraft(name: new string('a', 257));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithZeroEra_ReturnsValidationFailure()
    {
        // There is no century zero — a 0 is a data-entry mistake, not a valid era.
        var result = NewDraft(era: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(-41)]
    [InlineData(22)]
    [InlineData(1971)]
    public void Create_WithOutOfRangeEra_ReturnsValidationFailure(int era)
    {
        var result = NewDraft(era: era);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(12)]
    public void Create_WithSignedCenturyEra_IsAccepted(int era)
    {
        var result = NewDraft(era: era);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Era.Should().Be(era);
    }

    [Fact]
    public void Create_WithUndefinedCategory_ReturnsValidationFailure()
    {
        var result = NewDraft(category: (HistoricalFigureCategory)999);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedRole_ReturnsValidationFailure()
    {
        var result = NewDraft(role: (HistoricalFigureRole)999);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedTradition_ReturnsValidationFailure()
    {
        var result = NewDraft(tradition: (HistoricalFigureTradition)999);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_StampsCreatedAndUpdatedTimestamps()
    {
        var before = DateTimeOffset.UtcNow;

        var result = NewDraft();

        var after = DateTimeOffset.UtcNow;
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Value.UpdatedAt.Should().Be(result.Value.CreatedAt);
    }

    [Fact]
    public void Publish_WithTradition_SetsPublished()
    {
        var figure = PublishableDraft();

        var error = figure.Publish();

        error.Should().BeNull();
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
    }

    [Fact]
    public void Publish_WithoutTradition_ReturnsValidationAndStaysDraft()
    {
        var figure = NewDraft().Value!;

        var error = figure.Publish();

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        figure.Status.Should().Be(HistoricalFigureStatus.Draft);
    }

    [Fact]
    public void Publish_WithNotApplicableTradition_IsAccepted()
    {
        // "N/A" is a real answer for a pre-Christian figure, not a missing one.
        var figure = NewDraft(
            category: HistoricalFigureCategory.BiblicalOldTestament,
            era: -10,
            role: HistoricalFigureRole.King,
            region: HistoricalFigureRegion.IsraelJudah,
            tradition: HistoricalFigureTradition.NotApplicable).Value!;

        var error = figure.Publish();

        error.Should().BeNull();
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_IsIdempotent()
    {
        var figure = PublishableDraft();
        figure.Publish();

        var error = figure.Publish();

        error.Should().BeNull();
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
    }

    [Fact]
    public void SetPlayable_OnDraftFigure_ReturnsConflict()
    {
        var figure = PublishableDraft();

        var error = figure.SetPlayable(true);

        error.Should().NotBeNull();
        error!.Code.Should().Be("conflict");
        figure.PlayableInShmo.Should().BeFalse();
    }

    [Fact]
    public void SetPlayable_OnPublishedFigure_SetsFlag()
    {
        var figure = PublishableDraft();
        figure.Publish();

        var error = figure.SetPlayable(true);

        error.Should().BeNull();
        figure.PlayableInShmo.Should().BeTrue();
    }

    [Fact]
    public void SetPlayable_False_IsAllowedOnDraft()
    {
        var figure = PublishableDraft();

        var error = figure.SetPlayable(false);

        error.Should().BeNull();
        figure.PlayableInShmo.Should().BeFalse();
    }

    [Fact]
    public void ReturnToDraft_ClearsPublishedAndPlayable()
    {
        var figure = PublishableDraft();
        figure.Publish();
        figure.SetPlayable(true);

        figure.ReturnToDraft();

        figure.Status.Should().Be(HistoricalFigureStatus.Draft);
        figure.PlayableInShmo.Should().BeFalse();
    }

    [Fact]
    public void Update_ReplacesEditableFields()
    {
        var figure = PublishableDraft();

        var error = figure.Update(
            name: "Ephrem the Syrian",
            category: HistoricalFigureCategory.Patristic,
            era: 4,
            role: HistoricalFigureRole.Commentator,
            region: HistoricalFigureRegion.Mesopotamia,
            gender: HistoricalFigureGender.Male,
            tradition: HistoricalFigureTradition.EastSyriac);

        error.Should().BeNull();
        figure.Name.Should().Be("Ephrem the Syrian");
        figure.Era.Should().Be(4);
        figure.Role.Should().Be(HistoricalFigureRole.Commentator);
        figure.Region.Should().Be(HistoricalFigureRegion.Mesopotamia);
        figure.Tradition.Should().Be(HistoricalFigureTradition.EastSyriac);
    }

    [Fact]
    public void Update_OnDraftFigure_AllowsClearingTradition()
    {
        var figure = PublishableDraft();

        var error = figure.Update(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: null);

        error.Should().BeNull();
        figure.Tradition.Should().BeNull();
    }

    [Fact]
    public void Update_OnPublishedFigure_ClearingTradition_ReturnsValidationAndLeavesFigureUnchanged()
    {
        var figure = PublishableDraft();
        figure.Publish();

        var error = figure.Update(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: null);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
        figure.Tradition.Should().Be(HistoricalFigureTradition.WestSyriac);
    }

    [Fact]
    public void Update_WithInvalidEra_ReturnsValidationAndLeavesFigureUnchanged()
    {
        var figure = PublishableDraft();

        var error = figure.Update(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 0,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: HistoricalFigureTradition.WestSyriac);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        figure.Era.Should().Be(7);
    }

    [Fact]
    public void Update_DoesNotChangeStatusOrPlayable()
    {
        var figure = PublishableDraft();
        figure.Publish();
        figure.SetPlayable(true);

        var error = figure.Update(
            name: "Ephrem the Syrian",
            category: HistoricalFigureCategory.Patristic,
            era: 4,
            role: HistoricalFigureRole.Commentator,
            region: HistoricalFigureRegion.Mesopotamia,
            gender: HistoricalFigureGender.Male,
            tradition: HistoricalFigureTradition.WestSyriac);

        error.Should().BeNull();
        figure.Status.Should().Be(HistoricalFigureStatus.Published);
        figure.PlayableInShmo.Should().BeTrue();
    }

    [Fact]
    public void Update_BumpsUpdatedAt()
    {
        var figure = PublishableDraft();
        var original = figure.UpdatedAt;

        var error = figure.Update(
            name: JacobOfEdessa,
            category: HistoricalFigureCategory.Patristic,
            era: 7,
            role: HistoricalFigureRole.Bishop,
            region: HistoricalFigureRegion.Syria,
            gender: HistoricalFigureGender.Male,
            tradition: HistoricalFigureTradition.WestSyriac);

        error.Should().BeNull();
        figure.UpdatedAt.Should().BeOnOrAfter(original);
    }

    private static Result<HistoricalFigure> NewDraft(
        string name = JacobOfEdessa,
        HistoricalFigureCategory category = HistoricalFigureCategory.Patristic,
        int era = 7,
        HistoricalFigureRole role = HistoricalFigureRole.Bishop,
        HistoricalFigureRegion region = HistoricalFigureRegion.Syria,
        HistoricalFigureGender gender = HistoricalFigureGender.Male,
        HistoricalFigureTradition? tradition = null) =>
        HistoricalFigure.Create(name, category, era, role, region, gender, tradition);

    private static HistoricalFigure PublishableDraft() =>
        NewDraft(tradition: HistoricalFigureTradition.WestSyriac).Value!;
}
