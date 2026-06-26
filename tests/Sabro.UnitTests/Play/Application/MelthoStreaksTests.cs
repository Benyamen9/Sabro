using FluentAssertions;
using Sabro.Play.Application.Meltho;

namespace Sabro.UnitTests.Play.Application;

public class MelthoStreaksTests
{
    [Fact]
    public void Compute_WithNoGames_ReturnsZeroes()
    {
        var streak = MelthoStreaks.Compute(Array.Empty<(DateOnly, bool)>());

        streak.Longest.Should().Be(0);
        streak.Current.Should().Be(0);
    }

    [Fact]
    public void Compute_CountsTrailingConsecutiveWinsAsCurrentStreak()
    {
        var streak = MelthoStreaks.Compute(new[]
        {
            Game("2026-06-01", true),
            Game("2026-06-02", true),
            Game("2026-06-03", true),
        });

        streak.Current.Should().Be(3);
        streak.Longest.Should().Be(3);
    }

    [Fact]
    public void Compute_BreaksCurrentStreakOnALoss()
    {
        var streak = MelthoStreaks.Compute(new[]
        {
            Game("2026-06-01", true),
            Game("2026-06-02", false),
            Game("2026-06-03", true),
            Game("2026-06-04", true),
        });

        streak.Current.Should().Be(2);
    }

    [Fact]
    public void Compute_BreaksStreakWhenADayIsSkipped()
    {
        var streak = MelthoStreaks.Compute(new[]
        {
            Game("2026-06-01", true),
            Game("2026-06-02", true),

            // 06-03 skipped
            Game("2026-06-04", true),
            Game("2026-06-05", true),
        });

        streak.Longest.Should().Be(2);
        streak.Current.Should().Be(2);
    }

    [Fact]
    public void Compute_TracksLongestRunIndependentlyOfCurrent()
    {
        var streak = MelthoStreaks.Compute(new[]
        {
            Game("2026-06-01", true),
            Game("2026-06-02", true),
            Game("2026-06-03", true),
            Game("2026-06-04", false),
            Game("2026-06-05", true),
        });

        streak.Longest.Should().Be(3);
        streak.Current.Should().Be(1);
    }

    [Fact]
    public void Compute_IsOrderIndependent()
    {
        var streak = MelthoStreaks.Compute(new[]
        {
            Game("2026-06-04", true),
            Game("2026-06-01", true),
            Game("2026-06-03", true),
            Game("2026-06-02", true),
        });

        streak.Longest.Should().Be(4);
        streak.Current.Should().Be(4);
    }

    private static (DateOnly PlayedOn, bool Solved) Game(string date, bool solved) => (DateOnly.Parse(date), solved);
}
