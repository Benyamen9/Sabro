using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.SuggestedEdits;

/// <summary>
/// A reviewer's proposed new value for one field of a Lexicon entry or a
/// historical figure.
/// </summary>
/// <remarks>
/// There is no <c>TargetVersion</c> or <c>TargetUpdatedAt</c> here on purpose: the
/// server reads the target's timestamp from the owning module when the proposal is
/// filed. A caller able to supply it could claim to be proposing against the
/// current content when they are not, which would defeat the staleness warning the
/// Owner relies on.
/// </remarks>
public sealed record CreateFieldProposalRequest(
    SuggestedEditTargetType TargetType,
    Guid TargetId,
    string Field,
    string ProposedValue,
    string? Rationale = null);
