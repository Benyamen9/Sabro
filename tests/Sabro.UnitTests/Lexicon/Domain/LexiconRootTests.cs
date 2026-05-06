using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Domain;

public class LexiconRootTests
{
    private const string KtbRoot = "ܟܬܒ";

    [Fact]
    public void Create_WithValidSyriacForm_ReturnsSuccess()
    {
        var result = LexiconRoot.Create(KtbRoot);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Form.Should().Be(KtbRoot);
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingForm_ReturnsValidationFailure(string? form)
    {
        var result = LexiconRoot.Create(form!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithLatinForm_ReturnsValidationFailure()
    {
        var result = LexiconRoot.Create("ktb");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsForm()
    {
        var result = LexiconRoot.Create($"   {KtbRoot}   ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Form.Should().Be(KtbRoot);
    }

    [Fact]
    public void Create_NormalizesToNfc()
    {
        // NFD form of ܟܬܒ followed by a combining mark, ensure NFC normalization is applied.
        var nfd = KtbRoot.Normalize(System.Text.NormalizationForm.FormD);

        var result = LexiconRoot.Create(nfd);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Form.IsNormalized(System.Text.NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void Create_StampsCreatedAndUpdatedTimestamps()
    {
        var before = DateTimeOffset.UtcNow;

        var result = LexiconRoot.Create(KtbRoot);

        var after = DateTimeOffset.UtcNow;
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Value.UpdatedAt.Should().Be(result.Value.CreatedAt);
    }
}
