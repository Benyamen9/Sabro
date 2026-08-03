using Sabro.Shared.Results;

namespace Sabro.Shared.Abstractions;

/// <summary>
/// How a content module exposes one of its entities as something a reviewer may
/// propose corrections to.
/// </summary>
/// <remarks>
/// <para>
/// The Reviews module owns the proposal workflow but must not know what a Lexicon
/// entry or a historical figure <i>is</i> — modules talk through explicit public
/// interfaces, never direct references. This is that interface, and it lets Reviews
/// answer two questions it cannot answer alone: does the target exist, and when was
/// it last changed.
/// </para>
/// <para>
/// <b>The target type is a string, not an enum</b>, deliberately. An enum would have
/// to live somewhere both sides can see it, which would make Lexicon and Historical
/// depend on Reviews — the coupling this interface exists to avoid.
/// </para>
/// <para>
/// Each module also declares which of its own fields are proposable, because the
/// module that owns an entity is the only place that can say so correctly. Anything
/// absent from that list is refused, so publication state (<c>Status</c>,
/// <c>PlayableInMeltho</c>, <c>PlayableInShmo</c>) stays Owner-only by simply not
/// being listed — the business rule is enforced by omission rather than by a check
/// somebody has to remember to write.
/// </para>
/// </remarks>
public interface IProposalTargetSource
{
    /// <summary>
    /// Discriminator matching the Reviews module's target-type name, e.g.
    /// <c>"LexiconEntry"</c>.
    /// </summary>
    string TargetTypeName { get; }

    /// <summary>
    /// Field names a reviewer may propose a new value for, in the same camelCase
    /// spelling the API uses, so the backoffice can map a proposal straight onto a
    /// form control. Anything not listed here is rejected.
    /// </summary>
    IReadOnlyCollection<string> ProposableFields { get; }

    /// <summary>
    /// When the target was last modified, or <see langword="null"/> if no such target
    /// exists. Doubles as the existence check.
    /// </summary>
    Task<DateTimeOffset?> GetUpdatedAtAsync(Guid targetId, CancellationToken cancellationToken);

    /// <summary>
    /// The field's value as it stands right now, or <see langword="null"/> if the
    /// target is gone or the field is currently unset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read once when the proposal is filed and again when the Owner decides. If the
    /// two differ, that exact field changed while the proposal waited — which is what
    /// makes the staleness check precise rather than approximate.
    /// </para>
    /// <para>
    /// Comparing per field rather than on the entity's <c>UpdatedAt</c> is deliberate.
    /// A timestamp marks a pending French-gloss proposal stale because somebody edited
    /// the English one, and with 1,445 description texts to work through, warnings
    /// that are usually wrong train the reader to click past the ones that are right.
    /// </para>
    /// <para>
    /// It is also what gives the review queue a real before → after diff, and a third
    /// value to show when the live content has diverged from both.
    /// </para>
    /// </remarks>
    Task<string?> GetFieldValueAsync(Guid targetId, string field, CancellationToken cancellationToken);

    /// <summary>
    /// Names the given targets, so a queue of proposals can say what each one is
    /// about. Ids with no surviving target are simply absent from the result.
    /// </summary>
    /// <remarks>
    /// Batched deliberately: the review queue resolves a whole page at once, and a
    /// per-row lookup would turn one screen into one query per proposal.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, ProposalTargetLabel>> GetLabelsAsync(
        IReadOnlyCollection<Guid> targetIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes an accepted value onto the target, returning <see langword="null"/> on
    /// success or the error that stopped it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Goes through the owning module's normal write path, so validation,
    /// normalisation and search indexing all happen exactly as they would from the
    /// backoffice form. Reviews must never write another module's content itself.
    /// </para>
    /// <para>
    /// The caller has already checked that the field is proposable and that the
    /// target has not changed underneath. This does not re-decide any of that; it
    /// applies what the Owner accepted.
    /// </para>
    /// </remarks>
    Task<Error?> ApplyFieldAsync(
        Guid targetId,
        string field,
        string value,
        CancellationToken cancellationToken);
}
