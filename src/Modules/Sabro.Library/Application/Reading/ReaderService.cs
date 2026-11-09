using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Library.Domain;
using Sabro.Library.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Library.Application.Reading;

internal sealed class ReaderService : IReaderService
{
    private static readonly Error MissingUser = Error.Validation("LogtoUserId is required.");

    private readonly LibraryDbContext dbContext;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ReaderService> logger;

    public ReaderService(LibraryDbContext dbContext, TimeProvider timeProvider, ILogger<ReaderService> logger)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<ReaderShelf>> GetShelfAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var userId = TrimUser(logtoUserId);
        if (userId is null)
        {
            return Result<ReaderShelf>.Failure(MissingUser);
        }

        var places = await dbContext.ReadingPlaces
            .AsNoTracking()
            .Where(p => p.LogtoUserId == userId)
            .OrderBy(p => p.Book)
            .Select(p => new ReadingPlaceDto(p.Book, p.ItemKey, p.OpenedAt))
            .ToListAsync(cancellationToken);

        var marks = await dbContext.ReadingMarks
            .AsNoTracking()
            .Where(m => m.LogtoUserId == userId)
            .OrderByDescending(m => m.MarkedAt)
            .ThenBy(m => m.Book)
            .ThenBy(m => m.ItemKey)
            .Select(m => new ReadingMarkDto(m.Book, m.ItemKey, m.MarkedAt))
            .ToListAsync(cancellationToken);

        return Result<ReaderShelf>.Success(new ReaderShelf(places, marks));
    }

    public async Task<Result<ReadingPlaceDto>> SetPlaceAsync(string logtoUserId, string book, SetReadingPlaceRequest request, CancellationToken cancellationToken)
    {
        var userId = TrimUser(logtoUserId);
        if (userId is null)
        {
            return Result<ReadingPlaceDto>.Failure(MissingUser);
        }

        var key = Normalize(book, request?.ItemKey);
        if (!key.IsSuccess)
        {
            return Result<ReadingPlaceDto>.Failure(key.Error!);
        }

        var (normalizedBook, itemKey) = key.Value;
        var now = timeProvider.GetUtcNow();
        var openedAt = ClampToNow(request!.OpenedAt, now);

        for (var attempt = 0; ; attempt++)
        {
            var place = await dbContext.ReadingPlaces
                .FirstOrDefaultAsync(p => p.LogtoUserId == userId && p.Book == normalizedBook, cancellationToken);
            if (place is not null)
            {
                if (place.MoveTo(itemKey, openedAt, now))
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                return Result<ReadingPlaceDto>.Success(Map(place));
            }

            place = ReadingPlace.Create(userId, normalizedBook, itemKey, openedAt, now);
            dbContext.ReadingPlaces.Add(place);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<ReadingPlaceDto>.Success(Map(place));
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                // Two devices placed the first ribbon in this book at once and the other one won
                // the unique (user, book) key. Go round once more and move theirs instead.
                dbContext.Entry(place).State = EntityState.Detached;
            }
        }
    }

    public async Task<Result<MarkItemOutcome>> MarkAsync(string logtoUserId, string book, string itemKey, MarkItemRequest? request, CancellationToken cancellationToken)
    {
        var userId = TrimUser(logtoUserId);
        if (userId is null)
        {
            return Result<MarkItemOutcome>.Failure(MissingUser);
        }

        var key = Normalize(book, itemKey);
        if (!key.IsSuccess)
        {
            return Result<MarkItemOutcome>.Failure(key.Error!);
        }

        var (normalizedBook, normalizedItem) = key.Value;

        var existing = await FindMarkAsync(userId, normalizedBook, normalizedItem, cancellationToken);
        if (existing is not null)
        {
            return Result<MarkItemOutcome>.Success(new MarkItemOutcome(Map(existing), WasCreated: false));
        }

        var count = await dbContext.ReadingMarks.CountAsync(m => m.LogtoUserId == userId, cancellationToken);
        if (count >= ReadingMark.MaxPerUser)
        {
            logger.LogWarning("Mark refused: the reader is at the ceiling. Max={Max}", ReadingMark.MaxPerUser);
            return Result<MarkItemOutcome>.Failure(Error.Conflict($"At most {ReadingMark.MaxPerUser} items can be marked."));
        }

        var now = timeProvider.GetUtcNow();
        var mark = ReadingMark.Create(userId, normalizedBook, normalizedItem, ClampToNow(request?.MarkedAt, now), now);
        dbContext.ReadingMarks.Add(mark);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (user, book, item) key: it is marked, which is all that was asked.
            dbContext.Entry(mark).State = EntityState.Detached;
            var raced = await FindMarkAsync(userId, normalizedBook, normalizedItem, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Result<MarkItemOutcome>.Success(new MarkItemOutcome(Map(raced), WasCreated: false));
        }

        return Result<MarkItemOutcome>.Success(new MarkItemOutcome(Map(mark), WasCreated: true));
    }

    public async Task<Error?> UnmarkAsync(string logtoUserId, string book, string itemKey, CancellationToken cancellationToken)
    {
        var userId = TrimUser(logtoUserId);
        if (userId is null)
        {
            return MissingUser;
        }

        var key = Normalize(book, itemKey);
        if (!key.IsSuccess)
        {
            return key.Error;
        }

        var (normalizedBook, normalizedItem) = key.Value;
        await dbContext.ReadingMarks
            .Where(m => m.LogtoUserId == userId && m.Book == normalizedBook && m.ItemKey == normalizedItem)
            .ExecuteDeleteAsync(cancellationToken);
        return null;
    }

    public async Task<Result<int>> DeleteAllForUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var userId = TrimUser(logtoUserId);
        if (userId is null)
        {
            return Result<int>.Failure(MissingUser);
        }

        var marks = await dbContext.ReadingMarks.Where(m => m.LogtoUserId == userId).ExecuteDeleteAsync(cancellationToken);
        var places = await dbContext.ReadingPlaces.Where(p => p.LogtoUserId == userId).ExecuteDeleteAsync(cancellationToken);
        return Result<int>.Success(marks + places);
    }

    private static string? TrimUser(string? logtoUserId)
    {
        var trimmed = (logtoUserId ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static Result<(string Book, string ItemKey)> Normalize(string? book, string? itemKey)
    {
        var bookResult = LibraryItemKey.NormalizeBook(book);
        if (!bookResult.IsSuccess)
        {
            return Result<(string, string)>.Failure(bookResult.Error!);
        }

        var itemResult = LibraryItemKey.Normalize(bookResult.Value!, itemKey);
        return itemResult.IsSuccess
            ? Result<(string, string)>.Success((bookResult.Value!, itemResult.Value!))
            : Result<(string, string)>.Failure(itemResult.Error!);
    }

    /// <summary>A time sent by a device: missing or in the future (a wrong clock) means now.</summary>
    private static DateTimeOffset ClampToNow(DateTimeOffset? sent, DateTimeOffset now) =>
        sent is { } value && value != default && value <= now ? value : now;

    private static ReadingPlaceDto Map(ReadingPlace place) => new(place.Book, place.ItemKey, place.OpenedAt);

    private static ReadingMarkDto Map(ReadingMark mark) => new(mark.Book, mark.ItemKey, mark.MarkedAt);

    private Task<ReadingMark?> FindMarkAsync(string userId, string book, string itemKey, CancellationToken cancellationToken) =>
        dbContext.ReadingMarks
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.LogtoUserId == userId && m.Book == book && m.ItemKey == itemKey, cancellationToken);
}
