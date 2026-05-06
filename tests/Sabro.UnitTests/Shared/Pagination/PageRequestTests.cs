using Sabro.Shared.Pagination;

namespace Sabro.UnitTests.Shared.Pagination;

public class PageRequestTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    [InlineData(7, 200)]
    public void Validate_WithValidInputs_ReturnsNull(int page, int pageSize)
    {
        PageRequest.Validate(page, pageSize).Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_WithNonPositivePage_ReportsPageField(int page)
    {
        var error = PageRequest.Validate(page, 50);

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        error.Fields.Should().ContainKey("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePageSize_ReportsPageSizeField(int pageSize)
    {
        var error = PageRequest.Validate(1, pageSize);

        error.Should().NotBeNull();
        error!.Fields.Should().ContainKey("pageSize");
    }

    [Fact]
    public void Validate_WithPageSizeAboveMax_ReportsPageSizeField()
    {
        var error = PageRequest.Validate(1, PageRequest.MaxPageSize + 1);

        error.Should().NotBeNull();
        error!.Fields.Should().ContainKey("pageSize");
    }

    [Fact]
    public void Validate_WithBothInvalid_ReportsBothFields()
    {
        var error = PageRequest.Validate(0, 0);

        error.Should().NotBeNull();
        error!.Fields!.Keys.Should().BeEquivalentTo("page", "pageSize");
    }
}
