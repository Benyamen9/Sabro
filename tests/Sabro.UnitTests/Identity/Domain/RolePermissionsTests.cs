using Sabro.Identity.Domain;

namespace Sabro.UnitTests.Identity.Domain;

public class RolePermissionsTests
{
    [Fact]
    public void Owner_MayDoEverything()
    {
        RolePermissions.CanEditLexicon(Role.Owner).Should().BeTrue();
        RolePermissions.CanEditFigures(Role.Owner).Should().BeTrue();
        RolePermissions.CanAssignRoles(Role.Owner).Should().BeTrue();
    }

    [Fact]
    public void Reader_MayDoNothing()
    {
        RolePermissions.CanEditLexicon(Role.Reader).Should().BeFalse();
        RolePermissions.CanEditFigures(Role.Reader).Should().BeFalse();
        RolePermissions.CanAssignRoles(Role.Reader).Should().BeFalse();
        RolePermissions.CanViewAnyBackoffice(Role.Reader).Should().BeFalse();
    }

    [Fact]
    public void ShmoEditor_ReachesFiguresButNotTheLexicon()
    {
        // The whole point of the area roles: this is what "let someone edit the
        // characters without handing over the dictionary" means.
        RolePermissions.CanEditFigures(Role.ShmoEditor).Should().BeTrue();
        RolePermissions.CanEditLexicon(Role.ShmoEditor).Should().BeFalse();
        RolePermissions.CanViewLexiconBackoffice(Role.ShmoEditor).Should().BeFalse();
    }

    [Fact]
    public void LexiconEditor_ReachesTheLexiconButNotFigures()
    {
        RolePermissions.CanEditLexicon(Role.LexiconEditor).Should().BeTrue();
        RolePermissions.CanEditFigures(Role.LexiconEditor).Should().BeFalse();
        RolePermissions.CanViewFiguresBackoffice(Role.LexiconEditor).Should().BeFalse();
    }

    [Theory]
    [InlineData(Role.ShmoReviewer)]
    [InlineData(Role.LexiconReviewer)]
    public void Reviewers_MaySeeTheirAreaButNotChangeIt(Role role)
    {
        RolePermissions.CanViewAnyBackoffice(role).Should().BeTrue();
        RolePermissions.CanEditLexicon(role).Should().BeFalse();
        RolePermissions.CanEditFigures(role).Should().BeFalse();
    }

    [Fact]
    public void ShmoReviewer_SeesFiguresOnly()
    {
        RolePermissions.CanViewFiguresBackoffice(Role.ShmoReviewer).Should().BeTrue();
        RolePermissions.CanViewLexiconBackoffice(Role.ShmoReviewer).Should().BeFalse();
    }

    [Theory]
    [InlineData(Role.Reader)]
    [InlineData(Role.ExpertReviewer)]
    [InlineData(Role.LexiconEditor)]
    [InlineData(Role.LexiconReviewer)]
    [InlineData(Role.ShmoEditor)]
    [InlineData(Role.ShmoReviewer)]
    public void OnlyTheOwnerMayAssignRoles(Role role)
    {
        // Being trusted with content is not being trusted with who else gets in.
        RolePermissions.CanAssignRoles(role).Should().BeFalse();
    }

    [Fact]
    public void ExpertReviewer_IsNotAnAreaRole()
    {
        // Predates the area roles and belongs to the deferred Reviews module. It
        // must not accidentally have acquired backoffice access.
        RolePermissions.CanEditLexicon(Role.ExpertReviewer).Should().BeFalse();
        RolePermissions.CanEditFigures(Role.ExpertReviewer).Should().BeFalse();
        RolePermissions.CanViewAnyBackoffice(Role.ExpertReviewer).Should().BeFalse();
    }

    [Theory]
    [InlineData(Role.LexiconReviewer, true)]
    [InlineData(Role.ShmoReviewer, false)]
    [InlineData(Role.LexiconEditor, false)]
    [InlineData(Role.Owner, false)]
    [InlineData(Role.Reader, false)]
    public void OnlyTheLexiconReviewerProposesLexiconEdits(Role role, bool expected)
    {
        // An editor and the Owner change entries directly — a proposal from either
        // would be a decision waiting on its own author. And a Shmo reviewer must not
        // reach into the Lexicon: that separation is the whole point of area roles.
        RolePermissions.CanProposeLexiconEdit(role).Should().Be(expected);
    }

    [Theory]
    [InlineData(Role.ShmoReviewer, true)]
    [InlineData(Role.LexiconReviewer, false)]
    [InlineData(Role.ShmoEditor, false)]
    [InlineData(Role.Owner, false)]
    public void OnlyTheShmoReviewerProposesFigureEdits(Role role, bool expected)
    {
        RolePermissions.CanProposeFigureEdit(role).Should().Be(expected);
    }

    [Fact]
    public void OnlyTheOwnerDecidesProposals()
    {
        // "Only the Owner accepts or rejects proposals" — an editor changes content,
        // but whose correction stands is the Owner's scholarly judgement.
        RolePermissions.CanDecideProposals(Role.Owner).Should().BeTrue();

        foreach (var role in Enum.GetValues<Role>().Where(r => r != Role.Owner))
        {
            RolePermissions.CanDecideProposals(role).Should().BeFalse();
        }
    }

    [Fact]
    public void NoReviewerRoleCanEditDirectly()
    {
        // The distinction the whole workflow rests on: a reviewer proposes, an editor
        // changes. If a reviewer ever gains direct edit rights, the queue is bypassable.
        RolePermissions.CanEditLexicon(Role.LexiconReviewer).Should().BeFalse();
        RolePermissions.CanEditFigures(Role.ShmoReviewer).Should().BeFalse();
    }

    [Fact]
    public void EveryRoleIsAccountedFor()
    {
        // A new role defaults to "no permissions" only if someone remembered to
        // think about it. This fails when a role is added without a decision.
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
}
