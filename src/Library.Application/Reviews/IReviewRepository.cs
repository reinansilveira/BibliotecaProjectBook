using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Reviews;

public interface IReviewRepository
{
    Task<Review?> GetAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default);

    Task<ReviewView?> GetViewAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default);

    Task<PagedResult<ReviewView>> ListByBookAsync(Guid bookId, PageRequest request, CancellationToken cancellationToken = default);

    Task RefreshBookRatingAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    void Remove(Review review);
}
