using Sabro.Library.Domain;

namespace Sabro.UnitTests.Library.Domain;

public class ReadingPlaceTests
{
    private static readonly DateTimeOffset Monday = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MoveTo_ALaterOpening_MovesTheRibbon()
    {
        var place = ReadingPlace.Create("reader", LibraryBooks.Grammar, "past", Monday, Monday);
        var later = Monday.AddHours(1);

        place.MoveTo("present", later, later).Should().BeTrue();

        place.ItemKey.Should().Be("present");
        place.OpenedAt.Should().Be(later);
        place.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void MoveTo_AnEarlierOpening_LeavesTheRibbonWhereItIs()
    {
        // A phone that was signed out sends yesterday's place after the computer sent today's.
        var place = ReadingPlace.Create("reader", LibraryBooks.Grammar, "present", Monday, Monday);

        place.MoveTo("past", Monday.AddDays(-1), Monday.AddMinutes(5)).Should().BeFalse();

        place.ItemKey.Should().Be("present");
        place.OpenedAt.Should().Be(Monday);
        place.UpdatedAt.Should().Be(Monday);
    }
}
