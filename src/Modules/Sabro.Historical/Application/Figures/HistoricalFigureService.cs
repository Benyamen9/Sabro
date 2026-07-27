using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Historical.Domain;
using Sabro.Historical.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Historical.Application.Figures;

internal sealed class HistoricalFigureService : IHistoricalFigureService
{
    private readonly HistoricalDbContext dbContext;
    private readonly IValidator<CreateHistoricalFigureRequest> createValidator;
    private readonly IValidator<UpdateHistoricalFigureRequest> updateValidator;
    private readonly ILogger<HistoricalFigureService> logger;

    public HistoricalFigureService(
        HistoricalDbContext dbContext,
        IValidator<CreateHistoricalFigureRequest> createValidator,
        IValidator<UpdateHistoricalFigureRequest> updateValidator,
        ILogger<HistoricalFigureService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.logger = logger;
    }

    public async Task<Result<HistoricalFigureDto>> CreateAsync(CreateHistoricalFigureRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "HistoricalFigure creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<HistoricalFigureDto>.Failure(Error.Validation(fields));
        }

        var domainResult = HistoricalFigure.Create(
            request.Name,
            request.Category,
            request.Era,
            request.Role,
            request.Region,
            request.Gender,
            request.Tradition);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "HistoricalFigure creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<HistoricalFigureDto>.Failure(domainResult.Error!);
        }

        var figure = domainResult.Value!;
        dbContext.Figures.Add(figure);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "HistoricalFigure created. Id={FigureId} Name={Name} Category={Category}",
            figure.Id,
            figure.Name,
            figure.Category);

        return Result<HistoricalFigureDto>.Success(Map(figure));
    }

    public async Task<Result<HistoricalFigureDto>> UpdateAsync(Guid id, UpdateHistoricalFigureRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "HistoricalFigure update rejected at request validation. Id={FigureId} Fields={FieldNames}",
                id,
                fields.Keys);

            return Result<HistoricalFigureDto>.Failure(Error.Validation(fields));
        }

        var figure = await dbContext.Figures.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (figure is null)
        {
            return Result<HistoricalFigureDto>.Failure(Error.NotFound($"HistoricalFigure {id} not found."));
        }

        var error = figure.Update(
            request.Name,
            request.Category,
            request.Era,
            request.Role,
            request.Region,
            request.Gender,
            request.Tradition);
        if (error is not null)
        {
            logger.LogWarning(
                "HistoricalFigure update rejected by domain invariant. Id={FigureId} Code={ErrorCode} Message={ErrorMessage}",
                id,
                error.Code,
                error.Message);

            return Result<HistoricalFigureDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("HistoricalFigure updated. Id={FigureId}", figure.Id);

        return Result<HistoricalFigureDto>.Success(Map(figure));
    }

    public async Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var figure = await dbContext.Figures.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (figure is null)
        {
            return Error.NotFound($"HistoricalFigure {id} not found.");
        }

        dbContext.Figures.Remove(figure);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("HistoricalFigure deleted. Id={FigureId}", id);

        return null;
    }

    public Task<Result<HistoricalFigureDto>> PublishAsync(Guid id, CancellationToken cancellationToken) =>
        MutateAsync(id, figure => figure.Publish(), "published", cancellationToken);

    public Task<Result<HistoricalFigureDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken) =>
        MutateAsync(
            id,
            figure =>
            {
                figure.ReturnToDraft();
                return null;
            },
            "returned to draft",
            cancellationToken);

    public Task<Result<HistoricalFigureDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken) =>
        MutateAsync(id, figure => figure.SetPlayable(playable), $"playable={playable}", cancellationToken);

    public async Task<Result<HistoricalFigureDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var figure = await dbContext.Figures
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (figure is null)
        {
            return Result<HistoricalFigureDto>.Failure(Error.NotFound($"HistoricalFigure {id} not found."));
        }

        return Result<HistoricalFigureDto>.Success(Map(figure));
    }

    public async Task<Result<HistoricalFigureListItem>> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var figure = await dbContext.Figures
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.Status == HistoricalFigureStatus.Published, cancellationToken);
        if (figure is null)
        {
            return Result<HistoricalFigureListItem>.Failure(
                Error.NotFound("This figure is not in the roster."));
        }

        return Result<HistoricalFigureListItem>.Success(MapListItem(figure));
    }

    public async Task<Result<PagedResult<HistoricalFigureDto>>> ListAsync(
        string? search,
        HistoricalFigureStatus? status,
        HistoricalFigureCategory? category,
        HistoricalFigureRole? role,
        HistoricalFigureRegion? region,
        bool? playableInShmo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<HistoricalFigureDto>>.Failure(pageError);
        }

        var query = dbContext.Figures.AsNoTracking();

        var trimmedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(trimmedSearch))
        {
            query = query.Where(e => EF.Functions.ILike(e.Name, $"%{trimmedSearch}%"));
        }

        if (status is not null)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (category is not null)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        if (role is not null)
        {
            query = query.Where(e => e.Role == role.Value);
        }

        if (region is not null)
        {
            query = query.Where(e => e.Region == region.Value);
        }

        if (playableInShmo is not null)
        {
            query = query.Where(e => e.PlayableInShmo == playableInShmo.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<HistoricalFigureDto>>.Success(
            new PagedResult<HistoricalFigureDto>(items.Select(Map).ToArray(), total, page, pageSize));
    }

    public async Task<Result<PagedResult<HistoricalFigureListItem>>> ListPublishedAsync(
        HistoricalFigureCategory? category,
        HistoricalFigureRole? role,
        HistoricalFigureRegion? region,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<HistoricalFigureListItem>>.Failure(pageError);
        }

        var query = dbContext.Figures
            .AsNoTracking()
            .Where(e => e.Status == HistoricalFigureStatus.Published);

        if (category is not null)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        if (role is not null)
        {
            query = query.Where(e => e.Role == role.Value);
        }

        if (region is not null)
        {
            query = query.Where(e => e.Region == region.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<HistoricalFigureListItem>>.Success(
            new PagedResult<HistoricalFigureListItem>(items.Select(MapListItem).ToArray(), total, page, pageSize));
    }

    private static HistoricalFigureDto Map(HistoricalFigure figure) => new(
        figure.Id,
        figure.Name,
        figure.Category,
        figure.Era,
        figure.Role,
        figure.Region,
        figure.Tradition,
        figure.Gender,
        figure.Status,
        figure.PlayableInShmo,
        figure.CreatedAt,
        figure.UpdatedAt);

    private static HistoricalFigureListItem MapListItem(HistoricalFigure figure) => new(
        figure.Id,
        figure.Name,
        figure.Category,
        figure.Era,
        figure.Role,
        figure.Region,
        figure.Tradition,
        figure.Gender);

    private async Task<Result<HistoricalFigureDto>> MutateAsync(
        Guid id,
        Func<HistoricalFigure, Error?> mutate,
        string action,
        CancellationToken cancellationToken)
    {
        var figure = await dbContext.Figures.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (figure is null)
        {
            return Result<HistoricalFigureDto>.Failure(Error.NotFound($"HistoricalFigure {id} not found."));
        }

        var error = mutate(figure);
        if (error is not null)
        {
            logger.LogWarning(
                "HistoricalFigure state change rejected. Id={FigureId} Action={Action} Code={ErrorCode} Message={ErrorMessage}",
                id,
                action,
                error.Code,
                error.Message);

            return Result<HistoricalFigureDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("HistoricalFigure state changed. Id={FigureId} Action={Action}", id, action);

        return Result<HistoricalFigureDto>.Success(Map(figure));
    }
}
