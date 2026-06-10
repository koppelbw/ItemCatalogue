using Domain.Pagination;
using Shouldly;

namespace Domain.Tests.Pagination;

public class PageRequestTests
{
    [Fact]
    public void Create_WithNoArguments_UsesPageOneAndDefaultSize()
    {
        var request = PageRequest.Create();

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(PageRequest.DefaultPageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithMissingOrNonPositivePage_ClampsToOne(int? page)
    {
        PageRequest.Create(page: page).Page.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithMissingOrNonPositivePageSize_FallsBackToDefault(int? pageSize)
    {
        PageRequest.Create(pageSize: pageSize).PageSize.ShouldBe(PageRequest.DefaultPageSize);
    }

    [Fact]
    public void Create_WithPageSizeAboveMax_ClampsToMax()
    {
        PageRequest.Create(pageSize: PageRequest.MaxPageSize + 50).PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    [Fact]
    public void Create_WithPageSizeAtMax_KeepsExactValue()
    {
        PageRequest.Create(pageSize: PageRequest.MaxPageSize).PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    [Fact]
    public void Create_WithValidValues_KeepsThemUnchanged()
    {
        var request = PageRequest.Create(page: 3, pageSize: 25);

        request.Page.ShouldBe(3);
        request.PageSize.ShouldBe(25);
    }

    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 50, 100)]
    public void Skip_IsZeroBasedOffsetForThePage(int page, int pageSize, int expectedSkip)
    {
        PageRequest.Create(page, pageSize).Skip.ShouldBe(expectedSkip);
    }
}
