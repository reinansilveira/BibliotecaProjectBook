using Library.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IOrderedQueryable<T> query,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, request.Page, request.PageSize, totalItems);
    }
}
