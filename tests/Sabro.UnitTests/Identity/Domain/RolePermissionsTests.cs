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
