using Sabro.Translations.Infrastructure;

namespace Sabro.UnitTests.Translations.Infrastructure;

public class SnakeCaseNamingExtensionsTests
{
    [Theory]
    [InlineData("Code", "code")]
    [InlineData("Id", "id")]
    [InlineData("Name", "name")]
    [InlineData("ChapterNumber", "chapter_number")]
    [InlineData("TextVersionId", "text_version_id")]
    [InlineData("PreviousVersionId", "previous_version_id")]
    [InlineData("IsRightToLeft", "is_right_to_left")]
    [InlineData("MyURL", "my_url")]
    [InlineData("ABC", "abc")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData("already_snake", "already_snake")]
    public void ToSnakeCase_ConvertsAsExpected(string input, string expected)
    {
        var actual = SnakeCaseNamingExtensions.ToSnakeCase(input);

        actual.Should().Be(expected);
    }
}
