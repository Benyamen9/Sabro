using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.BethGazo.Application.Chants;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The public Beth Gazo surface, read by the Nahlo client and the hub's book page. Anonymous like the
/// dictionary, the Meltho library and the figure roster (public, non-personal
/// content; still rate-limited): Nahlo is played without an account.
/// </summary>
/// <remarks>
/// Deliberately narrow. The editorial listing lives behind
/// <c>/api/v1/admin/chants</c> and sees drafts, the playable flag and the recording
/// URLs; none of that belongs here, because the playable flag would enumerate the
/// puzzle pool.
/// </remarks>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/chants")]
public sealed class ChantsController : ApiControllerBase
{
    private readonly IPublicChantService chantService;

    public ChantsController(IPublicChantService chantService)
    {
        this.chantService = chantService;
    }

    /// <summary>
    /// The three lists a Nahlo round is answered against: melody names, modes and
    /// shuḥlofe, each on its own.
    /// </summary>
    /// <remarks>
    /// They are returned unjoined on purpose, and must stay that way: a listing that
    /// paired each melody with its mode would let a player who recognises the chant's
    /// text read the mode off the same row, and the mode is the skill the game
    /// teaches. Treat the lists as suggestions rather than a closed vocabulary — see
    /// <see cref="ChantAnswerOptionsDto"/>.
    /// </remarks>
    [HttpGet("answer-options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ChantAnswerOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChantAnswerOptionsDto>> GetAnswerOptions(CancellationToken cancellationToken) =>
        Ok(await chantService.GetAnswerOptionsAsync(cancellationToken));

    /// <summary>
    /// The whole Beth Gazo for the hub's book page: every section in order with its
    /// recorded chants grouped by melody, each playable, and every mode with its text.
    /// </summary>
    /// <remarks>
    /// Pairs every melody with its mode, by the Owner's ruling of 2026-10-25 that the
    /// treasury is open to anyone who wants to learn or listen, even though that lets
    /// a player look up today's Nahlo. See <see cref="BethGazoBookDto"/>.
    /// </remarks>
    [HttpGet("book")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BethGazoBookDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BethGazoBookDto>> GetBook(CancellationToken cancellationToken) =>
        Ok(await chantService.GetBookAsync(cancellationToken));
}
