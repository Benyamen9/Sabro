namespace Sabro.Play.Application.Shmo;

/// <summary>
/// Turns a player's recorded Shmo rounds into their collection. Pure, so the rules
/// are tested without a database.
/// </summary>
/// <remarks>
/// <para><b>Met</b>: every figure guessed in a round, plus the answer of a round given
/// up, since the reveal showed it.</para>
/// <para><b>Found</b>: the answer of a round solved. Found includes met.</para>
/// </remarks>
internal static class ShmoCollection
{
    public static IReadOnlyList<Card> Build(string logtoUserId, IEnumerable<ShmoPlayedDay> days)
    {
        var cards = new Dictionary<Guid, Card>();

        foreach (var day in days)
        {
            var seen = new HashSet<Guid>(day.GuessIds);
            if (day.AnswerId is { } revealed)
            {
                seen.Add(revealed);
            }

            foreach (var figureId in seen)
            {
                var card = cards.TryGetValue(figureId, out var existing) ? existing : new Card(figureId);
                cards[figureId] = card with { TimesMet = card.TimesMet + 1 };
            }

            if (day.Solved && day.AnswerId is { } answer)
            {
                var card = cards[answer];
                cards[answer] = card with
                {
                    Found = true,
                    FirstFoundOn = card.FirstFoundOn is { } first && first <= day.Date ? first : day.Date,
                    FewestAttempts = card.FewestAttempts is { } fewest ? Math.Min(fewest, day.Attempts) : day.Attempts,
                    Illuminated = card.Illuminated || ShmoIllumination.IsIlluminated(logtoUserId, answer, day.Date),
                };
            }
        }

        return cards.Values.ToArray();
    }

    internal sealed record Card(Guid FigureId)
    {
        public int TimesMet { get; init; }

        public bool Found { get; init; }

        public DateOnly? FirstFoundOn { get; init; }

        public int? FewestAttempts { get; init; }

        public bool Illuminated { get; init; }
    }
}
