using Library.Application.Common.Pagination;

namespace Library.Application.Audit;

public interface IAuditService
{
    Task<PagedResult<AuditEntryResponse>> ListAsync(AuditListRequest request, CancellationToken cancellationToken = default);
}
