using Sabro.Identity.Domain;

namespace Sabro.UnitTests.Identity.Domain;

public class UserProfileTests
{
    private const string LogtoUserId = "logto|abc123";

    [Fact]
    public void Create_WithLogtoUserIdOnly_DefaultsToEnglishAndEstrangela()
    {
        var result = UserProfile.Create(LogtoUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LogtoUserId.Should().Be(LogtoUserId);
        result.Value.PreferredLanguage.Should().Be("en");
        result.Value.PreferredScriptVariant.Should().Be(ScriptVariant.Estrangela);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingLogtoUserId_ReturnsValidationFailure(string? logtoUserId)
    {
        var result = UserProfile.Create(logtoUserId!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsLogtoUserId()
    {
        var result = UserProfile.Create($"  {LogtoUserId}  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.LogtoUserId.Should().Be(LogtoUserId);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("nl")]
    public void Create_WithSupportedLanguage_AcceptsIt(string language)
    {
        var result = UserProfile.Create(LogtoUserId, preferredLanguage: language);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreferredLanguage.Should().Be(language);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData(" En ")]
    public void Create_NormalizesLanguageToLowerTrimmed(string language)
    {
        var result = UserProfile.Create(LogtoUserId, preferredLanguage: language);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreferredLanguage.Should().Be("en");
    }

    [Theory]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("xx")]
    [InlineData("")]
    public void Create_WithUnsupportedLanguage_ReturnsValidationFailure(string language)
    {
        var result = UserProfile.Create(LogtoUserId, preferredLanguage: language);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_AcceptsExplicitScriptVariant()
    {
        var result = UserProfile.Create(LogtoUserId, preferredScriptVariant: ScriptVariant.Madnhaya);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreferredScriptVariant.Should().Be(ScriptVariant.Madnhaya);
    }

    [Fact]
    public void UpdatePreferences_WithValidValues_ReturnsNullAndMutatesProperties()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;
        var originalUpdatedAt = profile.UpdatedAt;

        var error = profile.UpdatePreferences("fr", ScriptVariant.Serto);

        error.Should().BeNull();
        profile.PreferredLanguage.Should().Be("fr");
        profile.PreferredScriptVariant.Should().Be(ScriptVariant.Serto);
        profile.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdatePreferences_WithUnsupportedLanguage_ReturnsErrorAndDoesNotMutate()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;
        var originalLanguage = profile.PreferredLanguage;
        var originalVariant = profile.PreferredScriptVariant;
        var originalUpdatedAt = profile.UpdatedAt;

        var error = profile.UpdatePreferences("xx", ScriptVariant.Serto);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        profile.PreferredLanguage.Should().Be(originalLanguage);
        profile.PreferredScriptVariant.Should().Be(originalVariant);
        profile.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Create_DefaultsRoleToReader()
    {
        var result = UserProfile.Create(LogtoUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(Role.Reader);
    }

    [Theory]
    [InlineData(Role.Owner)]
    [InlineData(Role.ExpertReviewer)]
    [InlineData(Role.Reader)]
    public void AssignRole_WithDefinedRole_ReturnsNullAndMutates(Role newRole)
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;
        var originalUpdatedAt = profile.UpdatedAt;

        var error = profile.AssignRole(newRole);

        error.Should().BeNull();
        profile.Role.Should().Be(newRole);
        profile.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void AssignRole_WithUndefinedRole_ReturnsValidationErrorAndDoesNotMutate()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;
        var originalRole = profile.Role;
        var originalUpdatedAt = profile.UpdatedAt;

        var error = profile.AssignRole((Role)999);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        profile.Role.Should().Be(originalRole);
        profile.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Create_DefaultsLeaderboardOptOutAndNoDisplayName()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;

        profile.DisplayName.Should().BeNull();
        profile.ShowOnLeaderboard.Should().BeFalse();
    }

    [Fact]
    public void UpdateAccount_WithNameAndOptIn_ReturnsNullAndMutates()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;
        var originalUpdatedAt = profile.UpdatedAt;

        var error = profile.UpdateAccount("  Ephrem  ", showOnLeaderboard: true);

        error.Should().BeNull();
        profile.DisplayName.Should().Be("Ephrem"); // trimmed
        profile.ShowOnLeaderboard.Should().BeTrue();
        profile.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateAccount_WithEmptyName_StoresNull()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;

        var error = profile.UpdateAccount("   ", showOnLeaderboard: false);

        error.Should().BeNull();
        profile.DisplayName.Should().BeNull();
    }

    [Fact]
    public void UpdateAccount_OptInWithoutName_ReturnsValidationErrorAndDoesNotMutate()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;

        var error = profile.UpdateAccount(null, showOnLeaderboard: true);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        profile.ShowOnLeaderboard.Should().BeFalse();
        profile.DisplayName.Should().BeNull();
    }

    [Fact]
    public void UpdateAccount_NameTooLong_ReturnsValidationError()
    {
        var profile = UserProfile.Create(LogtoUserId).Value!;

        var error = profile.UpdateAccount(new string('a', UserProfile.MaxDisplayNameLength + 1), showOnLeaderboard: false);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
    }
}
