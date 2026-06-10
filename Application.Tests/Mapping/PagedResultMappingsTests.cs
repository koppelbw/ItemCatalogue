using Application.Mapping;
using Domain.Pagination;
using Shouldly;

namespace Application.Tests.Mapping;

public class PagedResultMappingsTests
{
    [Fact]
    public void ToResponse_ProjectsItemsAndCarriesMetadata()
    {
        // 45 rows, page 2 of size 20 => 3 total pages, has both next and previous.
        var page = new PagedResult<int>([10, 20, 30], TotalCount: 45, Page: 2, PageSize: 20);

        var response = page.ToResponse(i => $"#{i}");

        response.Items.ShouldBe(["#10", "#20", "#30"]);
        response.TotalCount.ShouldBe(45);
        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(20);
        response.TotalPages.ShouldBe(3);
        response.HasNext.ShouldBeTrue();
        response.HasPrevious.ShouldBeTrue();
    }

    [Fact]
    public void ToResponse_WithEmptyPage_ProducesEmptyItems()
    {
        var page = new PagedResult<int>([], TotalCount: 0, Page: 1, PageSize: 20);

        var response = page.ToResponse(i => i.ToString());

        response.Items.ShouldBeEmpty();
        response.TotalPages.ShouldBe(0);
        response.HasNext.ShouldBeFalse();
        response.HasPrevious.ShouldBeFalse();
    }
}
