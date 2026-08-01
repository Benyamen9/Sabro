using Sabro.Identity.Domain;

namespace Sabro.UnitTests.Identity.Domain;

/// <summary>
/// Who may do what, now that area access is a per-area grant rather than a rung on
/// a single role ladder.
/// </summary>
public class RolePermissionsTests
{
    [Fact]
    public void TheOwnerMayDoEverythingWithoutAnyAreaGrant()
    {
        var owner = Profile(Role.Owner);

        RolePermissions.CanEdit(owner, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanEdit(owner, ContentArea.Shmo).Should().BeTrue();
        RolePermissions.CanViewAnyBackoffice(owner).Should().BeTrue();
        RolePermissions.CanAssignRoles(owner).Should().BeTrue();
        RolePermissions.CanDecideProposals(owner).Should().BeTrue();
    }

    [Fact]
    public void AReaderMayDoNothing()
    {
        var reader = Profile(Role.Reader);

        RolePermissions.CanEdit(reader, ContentArea.Lexicon).Should().BeFalse();
        RolePermissions.CanViewBackoffice(reader, ContentArea.Lexicon).Should().BeFalse();
        RolePermissions.CanViewAnyBackoffice(reader).Should().BeFalse();
        RolePermissions.CanAssignRoles(reader).Should().BeFalse();
        RolePermissions.CanDecideProposals(reader).Should().BeFalse();
    }

    [Fact]
    public void AccessInOneAreaGrantsNothingInAnother()
    {
        // The separation the whole model exists for.
        var shmoEditor = Profile(Role.Reader, (ContentArea.Shmo, AreaAccess.Editor));

        RolePermissions.CanEdit(shmoEditor, ContentArea.Shmo).Should().BeTrue();
        RolePermissions.CanEdit(shmoEditor, ContentArea.Lexicon).Should().BeFalse();
        RolePermissions.CanViewBackoffice(shmoEditor, ContentArea.Lexicon).Should().BeFalse();
    }

    [Fact]
    public void OnePersonMayHoldDifferentLevelsInDifferentAreas()
    {
        // The case a single role could not express, and the reason for the change:
        // review one body of content while editing another.
        var both = Profile(
            Role.Reader,
            (ContentArea.Shmo, AreaAccess.Reviewer),
            (ContentArea.Lexicon, AreaAccess.Editor));

        RolePermissions.CanEdit(both, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanPropose(both, ContentArea.Lexicon).Should().BeFalse();

        RolePermissions.CanEdit(both, ContentArea.Shmo).Should().BeFalse();
        RolePermissions.CanViewBackoffice(both, ContentArea.Shmo).Should().BeTrue();
        RolePermissions.CanPropose(both, ContentArea.Shmo).Should().BeTrue();
    }

    [Fact]
    public void AReviewerMayViewAndProposeButNeverEdit()
    {
        // If a reviewer ever gains direct edit rights the proposal queue becomes
        // bypassable, which defeats the point of having one.
        var reviewer = Profile(Role.Reader, (ContentArea.Lexicon, AreaAccess.Reviewer));

        RolePermissions.CanViewBackoffice(reviewer, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanPropose(reviewer, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanEdit(reviewer, ContentArea.Lexicon).Should().BeFalse();
    }

    [Fact]
    public void AnEditorEditsDirectlyAndDoesNotPropose()
    {
        // A proposal from someone who can just make the change would be a decision
        // waiting on its own author.
        var editor = Profile(Role.Reader, (ContentArea.Lexicon, AreaAccess.Editor));

        RolePermissions.CanEdit(editor, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanViewBackoffice(editor, ContentArea.Lexicon).Should().BeTrue();
        RolePermissions.CanPropose(editor, ContentArea.Lexicon).Should().BeFalse();
    }

    [Fact]
    public void TheOwnerIsNotAReviewerOfTheirOwnWork()
    {
        RolePermissions.CanPropose(Profile(Role.Owner), ContentArea.Lexicon).Should().BeFalse();
    }

    [Fact]
    public void OnlyTheOwnerDecidesProposalsOrGrantsAccess()
    {
        // Being trusted with content is not the same as being trusted with who else
        // gets in — an editor grant must imply neither.
        var editor = Profile(Role.Reader, (ContentArea.Lexicon, AreaAccess.Editor));

        RolePermissions.CanDecideProposals(editor).Should().BeFalse();
        RolePermissions.CanAssignRoles(editor).Should().BeFalse();
    }

    [Fact]
    public void ExpertReviewerIsNotAnAreaRole()
    {
        // Predates the area grants and belongs to the deferred Reviews module. It
        // must not accidentally have acquired backoffice access.
        var expert = Profile(Role.ExpertReviewer);

        RolePermissions.CanViewAnyBackoffice(expert).Should().BeFalse();
        RolePermissions.CanEdit(expert, ContentArea.Lexicon).Should().BeFalse();
        RolePermissions.CanProposeTranslationEdit(expert).Should().BeTrue();
    }

    [Fact]
    public void LegacyAreaRolesGrantNothingOnTheirOwn()
    {
        // After the backfill these values mean nothing: access comes from grants. A
        // row still carrying one must not confer access by accident.
        foreach (var legacy in new[]
                 {
                     Role.LexiconReviewer, Role.LexiconEditor,
                     Role.ShmoReviewer, Role.ShmoEditor,
                 })
        {
            var profile = Profile(legacy);
            RolePermissions.CanViewAnyBackoffice(profile).Should().BeFalse();
            RolePermissions.CanEdit(profile, ContentArea.Lexicon).Should().BeFalse();
            RolePermissions.CanEdit(profile, ContentArea.Shmo).Should().BeFalse();
        }
    }

    [Fact]
    public void EveryAreaIsAccountedFor()
    {
        // A new ContentArea defaults to "nobody may touch it" only if someone
        // remembered to think about it. This fails when one is added silently.
        Enum.GetValues<ContentArea>().Should().BeEquivalentTo(
            new[] { ContentArea.Lexicon, ContentArea.Shmo },
            "a new ContentArea needs a decision here and a row in the backoffice grid");
    }

    [Fact]
    public void EveryRoleIsAccountedFor()
    {
        // The four area roles remain in the enum because the column stores strings and
        // old rows may still carry them; they are backfilled into grants and are no
        // longer assignable.
        var known = new[]
        {
            Role.Reader, Role.ExpertReviewer,
            Role.LexiconReviewer, Role.LexiconEditor,
            Role.ShmoReviewer, Role.ShmoEditor,
            Role.Owner,
        };

        Enum.GetValues<Role>().Should().BeEquivalentTo(
            known,
            "a new Role needs an explicit decision in RolePermissions and a test here");
    }

    private static UserProfile Profile(Role role, params (ContentArea Area, AreaAccess Access)[] grants)
    {
        var profile = UserProfile.Create($"logto|{Guid.NewGuid():N}").Value!;
        profile.AssignRole(role);
        foreach (var (area, access) in grants)
        {
            profile.SetAreaAccess(area, access);
        }

        return profile;
    }
}
