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
}
