namespace Sabro.Reviews.Domain;

/// <summary>
/// Outcome of an Owner's review decision. There is no Pending state — the
/// approval row is created at decision time, so it always carries a verdict.
/// Re-evaluation appends a new row; the latest by <c>DecisionAt</c> wins.
/// </summary>
public enum ApprovalStatus
{
    Approved,
    Rejected,
}
