using Domain.Pagination;
using Shouldly;

namespace Domain.Tests.Pagination;

public class PagedResultTests
{
    private static PagedResult<string> Page(int totalCount, int page, int pageSize) =>
        new(Items: [], TotalCount: totalCount, Page: page, PageSize: pageSize);

    [Theory]
    [InlineData(0, 20, 0)]    // no rows -> no pages
    [InlineData(20, 20, 1)]   // exactly one full page
    [InlineData(21, 20, 2)]   // one over -> rounds up
    [InlineData(100, 30, 4)]  // 3 full pages + remainder
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expectedPages)
    {
        Page(totalCount, page: 1, pageSize).TotalPages.ShouldBe(expectedPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_IsZeroAndDoesNotDivideByZero()
    {
        Page(totalCount: 50, page: 1, pageSize: 0).TotalPages.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasPrevious_IsTrueOnlyBeyondFirstPage(int page, bool expected)
    {
        Page(totalCount: 100, page, pageSize: 20).HasPrevious.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, true)]   // page 1 of 5
    [InlineData(4, true)]   // page 4 of 5
    [InlineData(5, false)]  // last page
    public void HasNext_IsTrueWhileMorePagesRemain(int page, bool expected)
    {
        // 100 rows / 20 per page = 5 pages
        Page(totalCount: 100, page, pageSize: 20).HasNext.ShouldBe(expected);
    }

    [Fact]
    public void Items_AreCarriedThroughUnchanged()
    {
        var result = new PagedResult<string>(["a", "b", "c"], TotalCount: 3, Page: 1, PageSize: 20);

        result.Items.ShouldBe(["a", "b", "c"]);
        result.TotalCount.ShouldBe(3);
    }
}
