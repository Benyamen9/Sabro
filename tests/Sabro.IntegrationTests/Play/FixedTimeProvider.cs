namespace Sabro.IntegrationTests.Play;

/// <summary>A <see cref="TimeProvider"/> that always reports a fixed instant — lets puzzle-selection tests pin "today".</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateOnly today)
    {
        now = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => now;
}
