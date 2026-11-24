using Library.Application.Common.Pagination;

namespace Library.UnitTests.Application;

public class PaginationTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(25, 10, 3)]
    [InlineData(30, 10, 3)]
    [InlineData(1, 50, 1)]
    public void Total_pages_rounds_up(int totalItems, int pageSize, int expectedTotalPages)
    {
        var page = new PagedResult<int>([], 1, pageSize, totalItems);

        Assert.Equal(expectedTotalPages, page.TotalPages);
    }

    [Fact]
    public void Navigation_flags_reflect_the_position()
    {
        var middle = new PagedResult<int>([], 2, 10, 35);

        Assert.True(middle.HasPreviousPage);
        Assert.True(middle.HasNextPage);
        Assert.False(new PagedResult<int>([], 4, 10, 35).HasNextPage);
    }

    [Fact]
    public void Map_keeps_the_metadata()
    {
        var page = new PagedResult<int>([1, 2], 3, 2, 10).Map(number => number.ToString());

        Assert.Equal(["1", "2"], page.Items);
        Assert.Equal(3, page.Page);
        Assert.Equal(10, page.TotalItems);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void Invalid_page_request_is_rejected(int page, int pageSize)
    {
        var result = new PageRequestValidator().Validate(new PageRequest { Page = page, PageSize = pageSize });

        Assert.False(result.IsValid);
    }
}
