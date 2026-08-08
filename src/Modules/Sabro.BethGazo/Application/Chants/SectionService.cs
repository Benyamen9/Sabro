using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

internal sealed class SectionService : ISectionService
{
    private readonly BethGazoDbContext dbContext;
    private readonly ILogger<SectionService> logger;

    public SectionService(BethGazoDbContext dbContext, ILogger<SectionService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<Result<BethGazoSectionDto>> CreateAsync(
        SectionRequest request,
        CancellationToken cancellationToken)
    {
        var modeError = await CheckModesExistAsync(request.AllowedModeIds, cancellationToken);
        if (modeError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(modeError);
        }

        // Appended, never numbered by hand — see ISectionService. Max+1 rather than
        // Count+1: a delete leaves a gap, and Count would then collide with the row
        // already sitting at that position.
        var nextPosition = await dbContext.Sections.AnyAsync(cancellationToken)
            ? await dbContext.Sections.MaxAsync(s => s.Position, cancellationToken) + 1
            : 1;

        var created = BethGazoSection.Create(request.Name, nextPosition);
        if (!created.IsSuccess)
        {
            return Result<BethGazoSectionDto>.Failure(created.Error!);
        }

        var section = created.Value!;
        var allowedError = section.SetAllowedModes(request.AllowedModeIds);
        if (allowedError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(allowedError);
        }

        dbContext.Sections.Add(section);

        var saveError = await SaveGuardingNameAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(saveError);
        }

        logger.LogInformation(
            "Beth Gazo section created. SectionId={SectionId} Name={Name} Modes={ModeCount}",
            section.Id,
            section.Name,
            request.AllowedModeIds.Count);

        return await ProjectAsync(section.Id, cancellationToken);
    }

    public async Task<Result<BethGazoSectionDto>> UpdateAsync(
        Guid id,
        SectionRequest request,
        CancellationToken cancellationToken)
    {
        var section = await dbContext.Sections
            .Include(s => s.AllowedModes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (section is null)
        {
            return Result<BethGazoSectionDto>.Failure(Error.NotFound("Section not found."));
        }

        var modeError = await CheckModesExistAsync(request.AllowedModeIds, cancellationToken);
        if (modeError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(modeError);
        }

        // Taking a mode away from a section that still has chants in it would leave
        // those chants holding a mode their section says cannot exist — the exact
        // state Chant.Normalize refuses on write. Refuse it here rather than let the
        // data drift into something the domain would no longer accept.
        var removed = section.AllowedModeIds.Except(request.AllowedModeIds).ToList();
        if (removed.Count > 0)
        {
            var stranded = await dbContext.Chants
                .CountAsync(c => c.SectionId == id && c.ModeId != null && removed.Contains(c.ModeId.Value), cancellationToken);
            if (stranded > 0)
            {
                return Result<BethGazoSectionDto>.Failure(Error.Conflict(
                    $"{stranded} chant(s) in this section still use a mode you are removing. Move or delete them first."));
            }
        }

        var renameError = section.Rename(request.Name);
        if (renameError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(renameError);
        }

        var allowedError = section.SetAllowedModes(request.AllowedModeIds);
        if (allowedError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(allowedError);
        }

        var saveError = await SaveGuardingNameAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<BethGazoSectionDto>.Failure(saveError);
        }

        logger.LogInformation("Beth Gazo section updated. SectionId={SectionId}", id);
        return await ProjectAsync(id, cancellationToken);
    }

    public async Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var section = await dbContext.Sections.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (section is null)
        {
            return Error.NotFound("Section not found.");
        }

        var chants = await dbContext.Chants.CountAsync(c => c.SectionId == id, cancellationToken);
        if (chants > 0)
        {
            return Error.Conflict(
                $"{chants} chant(s) belong to this section. Move or delete them first.");
        }

        dbContext.Sections.Remove(section);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Beth Gazo section deleted. SectionId={SectionId}", id);
        return null;
    }

    public async Task<Error?> MoveAsync(Guid id, bool up, CancellationToken cancellationToken)
    {
        var section = await dbContext.Sections.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (section is null)
        {
            return Error.NotFound("Section not found.");
        }

        var neighbour = up
            ? await dbContext.Sections
                .Where(s => s.Position < section.Position)
                .OrderByDescending(s => s.Position)
                .FirstOrDefaultAsync(cancellationToken)
            : await dbContext.Sections
                .Where(s => s.Position > section.Position)
                .OrderBy(s => s.Position)
                .FirstOrDefaultAsync(cancellationToken);

        if (neighbour is null)
        {
            // Already at the end it was asked to move towards. Not an error: the
            // button is simply a no-op there, and returning one would make the UI
            // shout about a press that changed nothing.
            return null;
        }

        // Three steps, not two. Position carries a unique index, so assigning the
        // neighbour's slot directly would collide with the row still holding it —
        // the swap has to park one of them somewhere free first. Negative is safe:
        // Create refuses anything below 1, so no real row can be sitting there.
        var mine = section.Position;
        var theirs = neighbour.Position;

        section.MoveTo(-1);
        await dbContext.SaveChangesAsync(cancellationToken);

        neighbour.MoveTo(mine);
        await dbContext.SaveChangesAsync(cancellationToken);

        section.MoveTo(theirs);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Beth Gazo section moved. SectionId={SectionId} From={From} To={To}", id, mine, theirs);
        return null;
    }

    private async Task<Error?> CheckModesExistAsync(
        IReadOnlyList<Guid> modeIds,
        CancellationToken cancellationToken)
    {
        if (modeIds.Count == 0)
        {
            return null;
        }

        var known = await dbContext.Modes
            .Where(m => modeIds.Contains(m.Id))
            .CountAsync(cancellationToken);

        return known == modeIds.Distinct().Count()
            ? null
            : Error.Validation("One or more of those modes does not exist.");
    }

    /// <summary>
    /// Turns the unique-name violation into something an editor can act on. The name
    /// is unique so two sections cannot be told apart only by their id, and a raw
    /// 23505 would say none of that.
    /// </summary>
    private async Task<Error?> SaveGuardingNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("ix_beth_gazo_sections_name", StringComparison.Ordinal) == true)
        {
            logger.LogWarning("Beth Gazo section save rejected by the unique name constraint.");
            return Error.Conflict("A section with that name already exists.");
        }
    }

    private async Task<Result<BethGazoSectionDto>> ProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Sections
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new BethGazoSectionDto(
                s.Id,
                s.Name,
                s.Position,
                s.AllowedModes.Select(m => m.ModeId).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<BethGazoSectionDto>.Failure(Error.NotFound("Section not found."))
            : Result<BethGazoSectionDto>.Success(dto);
    }
}
