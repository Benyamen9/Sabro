namespace Sabro.Reviews.Application.SuggestedEdits;

/// <summary>
/// Body for the accept/reject endpoints. The deciding user is taken from
/// the authenticated principal, not from this payload, so callers cannot
/// impersonate. <see cref="Note"/> is an optional free-text rationale that
/// gets persisted alongside the decision.
/// </summary>
/// <param name="Note">Optional free-text rationale, persisted with the decision.</param>
/// <param name="AcceptChangedTarget">
/// Confirms accepting a field proposal whose field has changed since it was filed.
/// Defaults to <see langword="false"/>, so the risky case fails closed: accepting a
/// correction written against older content can silently overwrite a newer one, and
/// that must be a deliberate act rather than a banner somebody skimmed past. Ignored
/// on reject and on prose targets.
/// </param>
/// <param name="Apply">
/// Writes the proposed value onto the target as part of accepting, instead of only
/// recording the decision. Defaults to <see langword="false"/>, which keeps the
/// original two-step shape: the Owner accepts, then opens the entry with the value
/// prefilled and saves it in context. Both are wanted — a typo does not need the
/// second look, a gloss read beside its four siblings does. Ignored on reject.
/// </param>
public sealed record DecisionRequest(
    string? Note = null,
    bool AcceptChangedTarget = false,
    bool Apply = false);
