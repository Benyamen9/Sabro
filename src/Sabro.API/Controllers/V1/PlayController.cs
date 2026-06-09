using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Application.Meltho;
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
    private readonly IGameResultService gameResultService;

    public PlayController(IMelthoPuzzleService melthoPuzzleService, IGameResultService gameResultService)
    {
        this.melthoPuzzleService = melthoPuzzleService;
        this.gameResultService = gameResultService;
    }

    /// <summary>
    /// Returns today's Meltho puzzle (get-or-create per date; identical for all
    /// players; respects the anti-repetition window). Read scope is sufficient —
    /// a read-only client still needs today's word to play.
    /// </summary>
    [HttpGet("meltho/today")]
    [Authorize(Policy = AuthPolicies.Read)]
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

    /// <summary>Returns the authenticated player's own results, newest day first, paged.</summary>
    [HttpGet("results/me")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(PagedResult<GameResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<GameResultDto>>> GetMyResults(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await gameResultService.ListForUserAsync(logtoUserIdResult.Value!, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
