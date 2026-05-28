using Sabro.Biblical.Domain;

namespace Sabro.UnitTests.Biblical.Domain;

public class CrossReferenceTests
{
    [Fact]
    public void Create_WithValidFields_ReturnsSuccess()
    {
        var anchorId = Guid.NewGuid();
        var passageId = Guid.NewGuid();

        var result = CrossReference.Create(anchorId, passageId, ReferenceSource.Author, ReferenceKind.Quotation);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AnnotationAnchorId.Should().Be(anchorId);
        result.Value.PassageId.Should().Be(passageId);
        result.Value.Source.Should().Be(ReferenceSource.Author);
        result.Value.Kind.Should().Be(ReferenceKind.Quotation);
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.CreatedAt.Should().Be(result.Value.UpdatedAt);
    }

    [Theory]
    [InlineData(ReferenceSource.Author, ReferenceKind.Quotation)]
    [InlineData(ReferenceSource.Author, ReferenceKind.Allusion)]
    [InlineData(ReferenceSource.Editorial, ReferenceKind.Quotation)]
    [InlineData(ReferenceSource.Editorial, ReferenceKind.Allusion)]
    public void Create_AllowsEveryAxisCombination(ReferenceSource source, ReferenceKind kind)
    {
        var result = CrossReference.Create(Guid.NewGuid(), Guid.NewGuid(), source, kind);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Source.Should().Be(source);
        result.Value.Kind.Should().Be(kind);
    }

    [Fact]
    public void Create_WithEmptyAnchorId_ReturnsValidationFailure()
    {
        var result = CrossReference.Create(Guid.Empty, Guid.NewGuid(), ReferenceSource.Author, ReferenceKind.Quotation);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyPassageId_ReturnsValidationFailure()
    {
        var result = CrossReference.Create(Guid.NewGuid(), Guid.Empty, ReferenceSource.Author, ReferenceKind.Quotation);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedSource_ReturnsValidationFailure()
    {
        var result = CrossReference.Create(Guid.NewGuid(), Guid.NewGuid(), (ReferenceSource)999, ReferenceKind.Quotation);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedKind_ReturnsValidationFailure()
    {
        var result = CrossReference.Create(Guid.NewGuid(), Guid.NewGuid(), ReferenceSource.Author, (ReferenceKind)999);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
