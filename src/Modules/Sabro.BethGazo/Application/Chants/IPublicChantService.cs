namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// The anonymous read surface over the Beth Gazo, consumed by the Nahlo client and
/// the hub's book page.
/// Separate from <see cref="IChantService"/> because that one is the editorial
/// write path and sees drafts; nothing here ever leaves the published set.
/// </summary>
public interface IPublicChantService
{
    /// <summary>
    /// The three answer lists a Nahlo round is played against. See
    /// <see cref="ChantAnswerOptionsDto"/> — they are returned unjoined on purpose.
    /// </summary>
    Task<ChantAnswerOptionsDto> GetAnswerOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The whole book: every section in order with its published, recorded chants
    /// grouped by melody, and every mode. See <see cref="BethGazoBookDto"/> for why
    /// this, unlike the answer options, pairs melodies with modes.
    /// </summary>
    Task<BethGazoBookDto> GetBookAsync(CancellationToken cancellationToken);
}
