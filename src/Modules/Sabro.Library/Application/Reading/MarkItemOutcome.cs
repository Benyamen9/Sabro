namespace Sabro.Library.Application.Reading;

/// <summary>The mark as kept, and whether this call made it (false when it was marked already).</summary>
public sealed record MarkItemOutcome(ReadingMarkDto Mark, bool WasCreated);
