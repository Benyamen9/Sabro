using System.ComponentModel.DataAnnotations;
using Sabro.Shared.Infrastructure.Search;

namespace Sabro.UnitTests.Shared.Search;

public class MeilisearchOptionsTests
{
    [Fact]
    public void Validate_WithMinimalValidConfig_PassesAllRules()
    {
        var options = new MeilisearchOptions
        {
            Url = "http://localhost:7700",
        };

        var errors = Validate(options);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingUrl_FailsRequiredRule()
    {
        var options = new MeilisearchOptions
        {
            Url = string.Empty,
        };

        var errors = Validate(options);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(MeilisearchOptions.Url)));
    }

    [Fact]
    public void Validate_WithMalformedUrl_FailsUrlRule()
    {
        var options = new MeilisearchOptions
        {
            Url = "not-a-url",
        };

        var errors = Validate(options);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(MeilisearchOptions.Url)));
    }

    [Fact]
    public void Validate_WithRequestTimeoutBelowMinimum_FailsRangeRule()
    {
        var options = new MeilisearchOptions
        {
            Url = "http://localhost:7700",
            RequestTimeout = TimeSpan.FromMilliseconds(100),
        };

        var errors = Validate(options);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(MeilisearchOptions.RequestTimeout)));
    }

    [Fact]
    public void Validate_WithRequestTimeoutAboveMaximum_FailsRangeRule()
    {
        var options = new MeilisearchOptions
        {
            Url = "http://localhost:7700",
            RequestTimeout = TimeSpan.FromMinutes(5),
        };

        var errors = Validate(options);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(MeilisearchOptions.RequestTimeout)));
    }

    [Fact]
    public void Validate_WithEmptyMasterKey_PassesBecauseMasterKeyIsOptional()
    {
        var options = new MeilisearchOptions
        {
            Url = "http://localhost:7700",
            MasterKey = string.Empty,
        };

        var errors = Validate(options);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void WaitForTasks_DefaultsToFalse()
    {
        new MeilisearchOptions().WaitForTasks.Should().BeFalse();
    }

    private static List<ValidationResult> Validate(MeilisearchOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
