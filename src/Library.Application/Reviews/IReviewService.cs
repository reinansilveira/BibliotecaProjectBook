using Library.Application.Common.Pagination;

namespace Library.Application.Reviews;

public interface IReviewService
{
    Task<PagedResult<ReviewResponse>> ListByBookAsync(Guid bookId, PageRequest request, CancellationToken cancellationToken = default);

    Task<ReviewResponse?> GetMineAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default);

    Task<ReviewResponse> SaveAsync(Guid bookId, Guid memberId, ReviewRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default);
}
