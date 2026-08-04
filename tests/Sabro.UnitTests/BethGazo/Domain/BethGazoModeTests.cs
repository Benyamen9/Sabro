using Sabro.BethGazo.Domain;

namespace Sabro.UnitTests.BethGazo.Domain;

public class BethGazoModeTests
{
    [Fact]
    public void Create_TrimsTheNameAndKeepsThePosition()
    {
        var result = BethGazoMode.Create("  Qadmoyo  ", 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Qadmoyo");
        result.Value.Position.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Fails(string name)
    {
        var result = BethGazoMode.Create(name, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithAPositionBelowOne_Fails(int position)
    {
        var result = BethGazoMode.Create("Qadmoyo", position);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("position");
    }

    [Fact]
    public void Create_AcceptsAPositionBeyondEight()
    {
        // Eight is a floor, not a ceiling — the owner has sets with more than eight,
        // which is the whole reason a mode is a table row rather than an enum member.
        // A bound of eight here would quietly reintroduce the limit the reference
        // table exists to avoid.
        var ninth = BethGazoMode.Create("A ninth mode", 9);
        var twelfth = BethGazoMode.Create("A twelfth mode", 12);

        ninth.IsSuccess.Should().BeTrue();
        twelfth.IsSuccess.Should().BeTrue();
        twelfth.Value!.Position.Should().Be(12);
    }

    [Fact]
    public void Rename_ReplacesTheNameAndRejectsABlankOne()
    {
        var mode = BethGazoMode.Create("Qadmoyo", 1).Value!;

        mode.Rename("Qadmoyo (revised)").Should().BeNull();
        mode.Name.Should().Be("Qadmoyo (revised)");

        mode.Rename("  ").Should().NotBeNull();
        mode.Name.Should().Be("Qadmoyo (revised)");
    }
}
