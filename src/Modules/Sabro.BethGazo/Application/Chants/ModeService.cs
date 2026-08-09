using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

internal sealed class ModeService : IModeService
{
    private readonly BethGazoDbContext dbContext;
    private readonly ILogger<ModeService> logger;

    public ModeService(BethGazoDbContext dbContext, ILogger<ModeService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<Result<BethGazoModeDto>> CreateAsync(
        ModeRequest request,
        CancellationToken cancellationToken)
    {
        // Appended, never numbered by hand. Max+1 rather than Count+1: a delete
        // leaves a gap, and Count would then collide with the row already sitting
        // at that position.
        var nextPosition = await dbContext.Modes.AnyAsync(cancellationToken)
            ? await dbContext.Modes.MaxAsync(m => m.Position, cancellationToken) + 1
            : 1;

        var created = BethGazoMode.Create(request.Name, nextPosition);
        if (!created.IsSuccess)
        {
            return Result<BethGazoModeDto>.Failure(created.Error!);
        }

        var mode = created.Value!;
        dbContext.Modes.Add(mode);

        var saveError = await SaveGuardingNameAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<BethGazoModeDto>.Failure(saveError);
        }

        logger.LogInformation(
            "Beth Gazo mode created. ModeId={ModeId} Name={Name} Position={Position}",
            mode.Id,
            mode.Name,
            mode.Position);

        return Result<BethGazoModeDto>.Success(new BethGazoModeDto(mode.Id, mode.Name, mode.Position));
    }

    public async Task<Result<BethGazoModeDto>> UpdateAsync(
        Guid id,
        ModeRequest request,
        CancellationToken cancellationToken)
    {
        var mode = await dbContext.Modes.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mode is null)
        {
            return Result<BethGazoModeDto>.Failure(Error.NotFound("Mode not found."));
        }

        // A rename is safe at any time: chants and sections point at the id, never
        // the name, so correcting a transliteration disturbs nothing.
        var error = mode.Rename(request.Name);
        if (error is not null)
        {
            return Result<BethGazoModeDto>.Failure(error);
        }

        var saveError = await SaveGuardingNameAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<BethGazoModeDto>.Failure(saveError);
        }

        logger.LogInformation("Beth Gazo mode updated. ModeId={ModeId}", id);
        return Result<BethGazoModeDto>.Success(new BethGazoModeDto(mode.Id, mode.Name, mode.Position));
    }

    public async Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var mode = await dbContext.Modes.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mode is null)
        {
            return Error.NotFound("Mode not found.");
        }

        // Two references, two guards. A chant's is the obvious one; a section's is
        // easy to forget and would leave the section admitting a mode that no longer
        // exists — its link is Cascade, so the row would vanish silently rather than
        // refuse, and the section would quietly narrow.
        var chants = await dbContext.Chants.CountAsync(c => c.ModeId == id, cancellationToken);
        if (chants > 0)
        {
            return Error.Conflict($"{chants} chant(s) are in this mode. Move or delete them first.");
        }

        var sections = await dbContext.Sections
            .CountAsync(s => s.AllowedModes.Any(m => m.ModeId == id), cancellationToken);
        if (sections > 0)
        {
            return Error.Conflict(
                $"{sections} section(s) still admit this mode. Remove it from them first.");
        }

        dbContext.Modes.Remove(mode);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Beth Gazo mode deleted. ModeId={ModeId}", id);
        return null;
    }

    public async Task<Error?> MoveAsync(Guid id, bool up, CancellationToken cancellationToken)
    {
        var mode = await dbContext.Modes.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (mode is null)
        {
            return Error.NotFound("Mode not found.");
        }

        var neighbour = up
            ? await dbContext.Modes
                .Where(m => m.Position < mode.Position)
                .OrderByDescending(m => m.Position)
                .FirstOrDefaultAsync(cancellationToken)
            : await dbContext.Modes
                .Where(m => m.Position > mode.Position)
                .OrderBy(m => m.Position)
                .FirstOrDefaultAsync(cancellationToken);

        // Already at the end it was asked to move towards: a no-op rather than an
        // error, since shouting about a press that changed nothing is worse.
        if (neighbour is null)
        {
            return null;
        }

        // Three steps. Position is uniquely indexed, so one row has to park outside
        // the range before the other can take its slot. Negative is safe: Create
        // refuses anything below 1, so no real row sits there.
        var mine = mode.Position;
        var theirs = neighbour.Position;

        mode.MoveTo(-1);
        await dbContext.SaveChangesAsync(cancellationToken);

        neighbour.MoveTo(mine);
        await dbContext.SaveChangesAsync(cancellationToken);

        mode.MoveTo(theirs);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Beth Gazo mode moved. ModeId={ModeId} From={From} To={To}", id, mine, theirs);
        return null;
    }

    private async Task<Error?> SaveGuardingNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("ix_beth_gazo_modes_name", StringComparison.Ordinal) == true)
        {
            logger.LogWarning("Beth Gazo mode save rejected by the unique name constraint.");
            return Error.Conflict("A mode with that name already exists.");
        }
    }
}
