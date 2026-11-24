using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Blacklist;

public interface IBlacklistRepository
{
    Task<BlacklistEntry?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<PagedResult<BlacklistEntry>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);

    Task<bool> IsBlacklistedAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task AddAsync(BlacklistEntry entry, CancellationToken cancellationToken = default);

    void Remove(BlacklistEntry entry);
}
