using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

internal sealed class ChantService : IChantService
{
    private const int MaxPageSize = 100;

    private readonly BethGazoDbContext dbContext;
    private readonly IChantAudioStorage audioStorage;
    private readonly IValidator<CreateChantRequest> createValidator;
    private readonly IValidator<UpdateChantRequest> updateValidator;
    private readonly ILogger<ChantService> logger;

    public ChantService(
        BethGazoDbContext dbContext,
        IChantAudioStorage audioStorage,
        IValidator<CreateChantRequest> createValidator,
        IValidator<UpdateChantRequest> updateValidator,
        ILogger<ChantService> logger)
    {
        this.dbContext = dbContext;
        this.audioStorage = audioStorage;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.logger = logger;
    }

    public async Task<Result<ChantDto>> CreateAsync(CreateChantRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning("Chant creation rejected at request validation. Fields={FieldNames}", fields.Keys);
            return Result<ChantDto>.Failure(Error.Validation(fields));
        }

        var sectionResult = await LoadSectionAsync(request.SectionId, cancellationToken);
        if (!sectionResult.IsSuccess)
        {
            return Result<ChantDto>.Failure(sectionResult.Error!);
        }

        var referenceError = await CheckReferencesAsync(request.ModeId, request.InheritsMelodyFromId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ChantDto>.Failure(referenceError);
        }

        var domainResult = Chant.Create(
            request.SyriacIncipit,
            request.Transliteration,
            sectionResult.Value!,
            request.ModeId,
            request.SyriacIncipitVocalized,
            request.VariantKind,
            request.VariantNumber,
            request.InheritsMelodyFromId);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Chant creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<ChantDto>.Failure(domainResult.Error!);
        }

        var chant = domainResult.Value!;
        dbContext.Chants.Add(chant);

        var saveError = await SaveGuardingIdentityAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<ChantDto>.Failure(saveError);
        }

        logger.LogInformation(
            "Chant created. ChantId={ChantId} Transliteration={Transliteration} ModeId={ModeId}",
            chant.Id,
            chant.Transliteration,
            chant.ModeId);

        return await ProjectAsync(chant.Id, cancellationToken);
    }

    public async Task<Result<ChantDto>> UpdateAsync(Guid id, UpdateChantRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            return Result<ChantDto>.Failure(Error.Validation(fields));
        }

        var chant = await dbContext.Chants.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (chant is null)
        {
            return Result<ChantDto>.Failure(Error.NotFound("Chant not found."));
        }

        var sectionResult = await LoadSectionAsync(request.SectionId, cancellationToken);
        if (!sectionResult.IsSuccess)
        {
            return Result<ChantDto>.Failure(sectionResult.Error!);
        }

        var referenceError = await CheckReferencesAsync(request.ModeId, request.InheritsMelodyFromId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ChantDto>.Failure(referenceError);
        }

        var error = chant.Update(
            request.SyriacIncipit,
            request.Transliteration,
            sectionResult.Value!,
            request.ModeId,
            request.SyriacIncipitVocalized,
            request.VariantKind,
            request.VariantNumber,
            request.InheritsMelodyFromId);
        if (error is not null)
        {
            return Result<ChantDto>.Failure(error);
        }

        var saveError = await SaveGuardingIdentityAsync(cancellationToken);
        if (saveError is not null)
        {
            return Result<ChantDto>.Failure(saveError);
        }

        logger.LogInformation("Chant updated. ChantId={ChantId}", chant.Id);
        return await ProjectAsync(chant.Id, cancellationToken);
    }

    public async Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var chant = await dbContext.Chants.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (chant is null)
        {
            return Error.NotFound("Chant not found.");
        }

        // A solqin points at the melody it borrows, so deleting a parent out from
        // under one would break the link. Refuse with an explanation rather than
        // surfacing a foreign-key violation.
        var solqinCount = await dbContext.Chants.CountAsync(e => e.InheritsMelodyFromId == id, cancellationToken);
        if (solqinCount > 0)
        {
            return Error.Conflict(
                $"{solqinCount} chant(s) inherit this melody. Repoint or delete them first.");
        }

        dbContext.Chants.Remove(chant);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Chant deleted. ChantId={ChantId}", id);
        return null;
    }

    public async Task<Result<ChantDto>> PublishAsync(Guid id, CancellationToken cancellationToken) =>
        await MutateAsync(id, chant => chant.Publish(), "published", cancellationToken);

    public async Task<Result<ChantDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken) =>
        await MutateAsync(
            id,
            chant =>
            {
                chant.ReturnToDraft();
                return null;
            },
            "returned to draft",
            cancellationToken);

    public async Task<Result<ChantDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken) =>
        await MutateAsync(id, chant => chant.SetPlayable(playable), $"playable set to {playable}", cancellationToken);

    public async Task<Result<ChantDto>> UploadAudioAsync(
        Guid id,
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        var chant = await dbContext.Chants.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (chant is null)
        {
            return Result<ChantDto>.Failure(Error.NotFound("Chant not found."));
        }

        var previousUrl = chant.AudioUrl;
        var newUrl = await audioStorage.SaveAsync(id, content, extension, cancellationToken);

        var error = chant.SetAudioUrl(newUrl);
        if (error is not null)
        {
            // The file is already written; drop it rather than leaving an orphan
            // nothing points at.
            audioStorage.Delete(newUrl);
            return Result<ChantDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Replace, not accumulate: drop the old file once the new one is recorded.
        // Guarded on inequality because re-uploading the same format overwrites in
        // place — the name is the chant id — and deleting then would remove the file
        // just saved.
        if (previousUrl is not null && previousUrl != newUrl)
        {
            audioStorage.Delete(previousUrl);
        }

        logger.LogInformation("Chant recording uploaded. ChantId={ChantId}", id);
        return await ProjectAsync(id, cancellationToken);
    }

    public async Task<Result<ChantDto>> RemoveAudioAsync(Guid id, CancellationToken cancellationToken)
    {
        var chant = await dbContext.Chants.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (chant is null)
        {
            return Result<ChantDto>.Failure(Error.NotFound("Chant not found."));
        }

        var previousUrl = chant.AudioUrl;

        // Refused while published, by the domain — a published chant without audio
        // would sit in the pool as an unplayable puzzle.
        var error = chant.SetAudioUrl(null);
        if (error is not null)
        {
            return Result<ChantDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousUrl is not null)
        {
            audioStorage.Delete(previousUrl);
        }

        logger.LogInformation("Chant recording removed. ChantId={ChantId}", id);
        return await ProjectAsync(id, cancellationToken);
    }

    public async Task<Result<ChantDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Chants.AsNoTracking().AnyAsync(e => e.Id == id, cancellationToken);
        return exists
            ? await ProjectAsync(id, cancellationToken)
            : Result<ChantDto>.Failure(Error.NotFound("Chant not found."));
    }

    public async Task<Result<PagedResult<ChantDto>>> ListAsync(
        string? search,
        ChantStatus? status,
        Guid? sectionId,
        Guid? modeId,
        bool? playableInNahlo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return Result<PagedResult<ChantDto>>.Failure(Error.Validation("Page must be 1 or greater."));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            return Result<PagedResult<ChantDto>>.Failure(
                Error.Validation($"PageSize must be between 1 and {MaxPageSize}."));
        }

        var query = dbContext.Chants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();

            // Matches the transliteration or the Syriac, so an editor can find an
            // entry by whichever form is to hand.
            query = query.Where(e =>
                EF.Functions.ILike(e.Transliteration, $"%{needle}%")
                || EF.Functions.ILike(e.SyriacIncipit, $"%{needle}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (sectionId.HasValue)
        {
            query = query.Where(e => e.SectionId == sectionId.Value);
        }

        if (modeId.HasValue)
        {
            query = query.Where(e => e.ModeId == modeId.Value);
        }

        if (playableInNahlo.HasValue)
        {
            query = query.Where(e => e.PlayableInNahlo == playableInNahlo.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await Project(query.OrderByDescending(e => e.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ChantDto>>.Success(new PagedResult<ChantDto>(items, total, page, pageSize));
    }

    public async Task<IReadOnlyList<BethGazoModeDto>> ListModesAsync(CancellationToken cancellationToken) =>
        await dbContext.Modes
            .AsNoTracking()
            .OrderBy(m => m.Position)
            .Select(m => new BethGazoModeDto(m.Id, m.Name, m.Position))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BethGazoSectionDto>> ListSectionsAsync(CancellationToken cancellationToken) =>
        await dbContext.Sections
            .AsNoTracking()
            .OrderBy(s => s.Position)
            .Select(s => new BethGazoSectionDto(
                s.Id,
                s.Name,
                s.Position,
                s.AllowedModes.Select(m => m.ModeId).ToList()))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Loads the section <b>with its allowed modes</b>, which is the whole point:
    /// the domain asks it whether the chant's mode is permitted, so a section
    /// fetched without that collection would report every mode as disallowed.
    /// </summary>
    private async Task<Result<BethGazoSection>> LoadSectionAsync(Guid sectionId, CancellationToken cancellationToken)
    {
        if (sectionId == Guid.Empty)
        {
            return Result<BethGazoSection>.Failure(Error.Validation("A section is required."));
        }

        var section = await dbContext.Sections
            .Include(s => s.AllowedModes)
            .FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);

        return section is null
            ? Result<BethGazoSection>.Failure(Error.Validation("That section does not exist."))
            : Result<BethGazoSection>.Success(section);
    }

    /// <summary>
    /// Checks the two foreign keys before the domain runs, so a bad id comes back as
    /// a field error rather than as a database constraint violation.
    /// </summary>
    private async Task<Error?> CheckReferencesAsync(Guid? modeId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (modeId.HasValue
            && modeId.Value != Guid.Empty
            && !await dbContext.Modes.AnyAsync(m => m.Id == modeId.Value, cancellationToken))
        {
            return Error.Validation("That mode does not exist.");
        }

        if (parentId.HasValue
            && parentId.Value != Guid.Empty
            && !await dbContext.Chants.AnyAsync(c => c.Id == parentId.Value, cancellationToken))
        {
            return Error.Validation("The chant this one inherits its melody from does not exist.");
        }

        return null;
    }

    /// <summary>
    /// Turns the identity unique-index violation into the message an editor needs.
    /// The constraint is the point of the schema — a melody name recurs across modes
    /// and shuḥlofe, so only the triple is unique — and a raw 23505 would say none
    /// of that.
    /// </summary>
    private async Task<Error?> SaveGuardingIdentityAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_chants_identity", StringComparison.Ordinal) == true)
        {
            logger.LogWarning("Chant save rejected by the identity constraint.");
            return Error.Conflict(
                "A chant with that melody name, section, mode and shuḥlofo already exists.");
        }
    }

    private async Task<Result<ChantDto>> MutateAsync(
        Guid id,
        Func<Chant, Error?> mutate,
        string what,
        CancellationToken cancellationToken)
    {
        var chant = await dbContext.Chants.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (chant is null)
        {
            return Result<ChantDto>.Failure(Error.NotFound("Chant not found."));
        }

        var error = mutate(chant);
        if (error is not null)
        {
            return Result<ChantDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Chant {What}. ChantId={ChantId}", what, id);
        return await ProjectAsync(id, cancellationToken);
    }

    private async Task<Result<ChantDto>> ProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await Project(dbContext.Chants.AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<ChantDto>.Failure(Error.NotFound("Chant not found."))
            : Result<ChantDto>.Success(dto);
    }

    /// <remarks>
    /// The mode is joined with <c>DefaultIfEmpty</c> — a LEFT join — and that is
    /// load-bearing, not tidiness. It used to be an inner join, which was correct
    /// only while every chant had a mode. Now that the madroshe have none, an inner
    /// join would drop every one of them from the backoffice list and from
    /// <c>GetById</c>: they would look deleted rather than mode-less.
    /// </remarks>
    private IQueryable<ChantDto> Project(IQueryable<Chant> query) =>
        from chant in query
        join section in dbContext.Sections.AsNoTracking() on chant.SectionId equals section.Id
        join mode in dbContext.Modes.AsNoTracking() on chant.ModeId equals mode.Id into modes
        from mode in modes.DefaultIfEmpty()
        join parent in dbContext.Chants.AsNoTracking()
            on chant.InheritsMelodyFromId equals parent.Id into parents
        from parent in parents.DefaultIfEmpty()
        select new ChantDto(
            chant.Id,
            chant.SyriacIncipit,
            chant.SyriacIncipitVocalized,
            chant.Transliteration,
            chant.SectionId,
            section.Name,
            chant.ModeId,
            mode != null ? mode.Name : null,
            chant.VariantKind,
            chant.VariantNumber,
            chant.InheritsMelodyFromId,
            parent != null ? parent.Transliteration : null,
            chant.AudioUrl,
            chant.Status,
            chant.PlayableInNahlo,
            chant.CreatedAt,
            chant.UpdatedAt);
}
