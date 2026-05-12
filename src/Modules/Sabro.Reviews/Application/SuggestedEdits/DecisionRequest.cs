namespace Sabro.Reviews.Application.SuggestedEdits;

/// <summary>
/// Body for the accept/reject endpoints. The deciding user is taken from
/// the authenticated principal, not from this payload, so callers cannot
/// impersonate. <see cref="Note"/> is an optional free-text rationale that
/// gets persisted alongside the decision.
/// </summary>
public sealed record DecisionRequest(string? Note = null);
