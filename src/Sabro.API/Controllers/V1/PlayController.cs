using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Application.Mno;
using Sabro.Play.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Ecosystem play surface: the shared Meltho daily puzzle plus the authenticated
/// player's own game results. Sabro owns only the shared daily-word selection;
/// clients (Meltho) own guess evaluation and presentation.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/play")]
public sealed class PlayController : ApiControllerBase
{
    private readonly IMelthoPuzzleService melthoPuzzleService;
    private readonly IMelthoLibraryService melthoLibraryService;
    private readonly IGameResultService gameResultService;
    private readonly IMelthoLeaderboardService melthoLeaderboardService;
    private readonly IMnoPuzzleService mnoPuzzleService;

    public PlayController(
        IMelthoPuzzleService melthoPuzzleService,
        IMelthoLibraryService melthoLibraryService,
        IGameResultService gameResultService,
        IMelthoLeaderboardService melthoLeaderboardService,
        IMnoPuzzleService mnoPuzzleService)
    {
        this.melthoPuzzleService = melthoPuzzleService;
        this.melthoLibraryService = melthoLibraryService;
        this.gameResultService = gameResultService;
        this.melthoLeaderboardService = melthoLeaderboardService;
        this.mnoPuzzleService = mnoPuzzleService;
    }

    /// <summary>
    /// Returns today's Meltho puzzle (get-or-create per date; identical for all
    /// players; respects the anti-repetition window). Public: anyone can play
    /// without an account — login is only needed to persist a result. The daily
    /// word is shared, non-personal content, so it is served anonymously (still
    /// rate-limited as a public endpoint).
    /// </summary>
    [HttpGet("meltho/today")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MelthoPuzzleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MelthoPuzzleDto>> GetTodaysMelthoPuzzle(CancellationToken cancellationToken)
    {
        var result = await melthoPuzzleService.GetTodaysPuzzleAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns today's Mno puzzle for the requested ladder level (get-or-create
    /// per date and difficulty; identical for every player on the level). Valid
    /// difficulty values: beginner, easy, normal, hard, extreme — normal when
    /// omitted, so pre-ladder clients keep working. The full solution ships
    /// with the puzzle: like Meltho's word, guess evaluation is client logic
    /// and per-tile feedback needs the exact board form. Public: anyone can
    /// play without an account — login is only needed to persist a result.
    /// Rate-limited as a public endpoint.
    /// </summary>
    [HttpGet("mno/today")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MnoPuzzleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MnoPuzzleDto>> GetTodaysMnoPuzzle(
        [FromQuery] MnoDifficulty difficulty = MnoDifficulty.Normal,
        CancellationToken cancellationToken = default)
    {
        var result = await mnoPuzzleService.GetTodaysPuzzleAsync(difficulty, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lists the public Meltho word library: words served on past days, paged, in the requested
    /// <paramref name="sort"/> order (most recent first by default). Today's word is never
    /// included (it would spoil the live puzzle). Public, non-personal content, so served
    /// anonymously (still rate-limited). Valid sort values: recent, alphabetical, length.
    /// An omitted direction applies the sort's natural default (recent → descending, others →
    /// ascending). An optional <paramref name="search"/> filters by Syriac form, transliteration,
    /// or gloss (case- and diacritic-insensitive).
    /// </summary>
    [HttpGet("meltho/library")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<MelthoLibraryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MelthoLibraryEntryDto>>> GetMelthoLibrary(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] LibrarySort sort = LibrarySort.Recent,
        [FromQuery] SortDirection? direction = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await melthoLibraryService.ListAsync(page, pageSize, sort, direction, search, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the detail for one Meltho word: the info table, the per-letter composition
    /// (qushoyo/rukkokho), and every date it was served (including today). Public. Unlike the
    /// browse list, this resolves today's word too: the id is only handed out by today's puzzle,
    /// so a caller who has it has already played and need not wait until tomorrow. 404 if the word
    /// has never been served.
    /// </summary>
    [HttpGet("meltho/library/{lexiconEntryId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MelthoLibraryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MelthoLibraryDetailDto>> GetMelthoLibraryWord(
        Guid lexiconEntryId,
        CancellationToken cancellationToken)
    {
        var result = await melthoLibraryService.GetDetailAsync(lexiconEntryId, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Records the authenticated player's result for one game on one day.
    /// Idempotent on (user, game, day): a repeat returns the existing row with 200,
    /// a first submission returns 201.
    /// </summary>
    [HttpPost("results")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(GameResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GameResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameResultDto>> RecordResult(RecordGameResultRequest request, CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await gameResultService.RecordAsync(logtoUserIdResult.Value!, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var outcome = result.Value!;
        if (outcome.WasCreated)
        {
            return StatusCode(StatusCodes.Status201Created, outcome.Result);
        }

        return Ok(outcome.Result);
    }

    /// <summary>
    /// Returns the Meltho leaderboard for the signed-in caller: the top players by longest
    /// streak (opted-in players only) plus the caller's own standing (streak always shown,
    /// even outside the top or when not opted in). Signed-in only — viewing requires a token;
    /// appearing additionally requires the player to opt in on their profile.
    /// </summary>
    [HttpGet("meltho/leaderboard")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(MelthoLeaderboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MelthoLeaderboardDto>> GetMelthoLeaderboard(CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await melthoLeaderboardService.GetAsync(logtoUserIdResult.Value!, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the authenticated player's own results, newest day first, paged.
    /// An optional <paramref name="gameId"/> restricts the list to one game
    /// (e.g. "meltho", "mno") — omit it for the cross-game profile view.
    /// </summary>
    [HttpGet("results/me")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(PagedResult<GameResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<GameResultDto>>> GetMyResults(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await gameResultService.ListForUserAsync(logtoUserIdResult.Value!, page, pageSize, gameId, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
