namespace Sabro.Identity.Domain;

/// <summary>
/// Authorization role attached to a <see cref="UserProfile"/>. There is one
/// Owner (the translator) at MVP; <see cref="ExpertReviewer"/> is assigned to
/// invited reviewers who may propose edits; <see cref="Reader"/> is the
/// default for any authenticated user. Stored as a string in the database so
/// adding a fourth role later does not require enum-value reshuffling.
/// </summary>
public enum Role
{
    Reader,
    ExpertReviewer,
    Owner,
}
