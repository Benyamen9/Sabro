using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Public;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Shmo;

internal sealed class ShmoCollectionService : IShmoCollectionService
{
    private readonly PlayDbContext dbContext;
    private readonly IHistoricalFigureDescriptions descriptions;

    public ShmoCollectionService(PlayDbContext dbContext, IHistoricalFigureDescriptions descriptions)
    {
        this.dbContext = dbContext;
        this.descriptions = descriptions;
    }

    public async Task<Result<ShmoCollectionDto>> GetForUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<ShmoCollectionDto>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var results = await dbContext.GameResults
            .AsNoTracking()
            .Where(r => r.LogtoUserId == trimmedUserId && r.GameId == Games.Shmo)
            .Select(r => new { r.PlayedOn, r.Solved, r.Attempts, r.DetailJson })
            .ToListAsync(cancellationToken);

        var dates = results.Select(r => r.PlayedOn).ToArray();
        var answers = await dbContext.ShmoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Shmo && dates.Contains(p.Date))
            .ToDictionaryAsync(p => p.Date, p => p.HistoricalFigureId, cancellationToken);

        var days = results.Select(r => new ShmoPlayedDay(
            r.PlayedOn,
            r.Solved,
            r.Attempts,
            ShmoResultDetail.GuessIds(r.DetailJson),
            answers.TryGetValue(r.PlayedOn, out var answer) ? answer : null));

        var cards = ShmoCollection.Build(trimmedUserId, days);

        var foundIds = cards.Where(c => c.Found).Select(c => c.FigureId).ToArray();
        var stories = await descriptions.GetDescriptionsAsync(foundIds, cancellationToken);

        var dto = new ShmoCollectionDto(cards
            .OrderBy(c => c.FigureId)
            .Select(c => new ShmoCollectionCardDto(
                c.FigureId,
                c.Found ? ShmoCardStatus.Found : ShmoCardStatus.Met,
                c.TimesMet,
                c.FirstFoundOn,
                c.FewestAttempts,
                c.Illuminated,
                StoryOf(c, stories)))
            .ToArray());

        return Result<ShmoCollectionDto>.Success(dto);
    }

    /// <summary>Only a found card reveals the story.</summary>
    private static ShmoCardDescriptionDto[] StoryOf(
        ShmoCollection.Card card,
        IReadOnlyDictionary<Guid, IReadOnlyList<FigureDescription>> stories) =>
        card.Found && stories.TryGetValue(card.FigureId, out var story)
            ? story.Select(d => new ShmoCardDescriptionDto(d.Language, d.Text)).ToArray()
            : Array.Empty<ShmoCardDescriptionDto>();
}
