using Library.Application.Common.Pagination;

namespace Library.Application.Blacklist;

public interface IBlacklistService
{
    Task<int> RefreshAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<BlacklistEntryResponse>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid memberId, CancellationToken cancellationToken = default);
}
