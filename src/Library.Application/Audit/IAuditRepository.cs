using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Audit;

public interface IAuditRepository
{
    Task<PagedResult<AuditEntry>> ListAsync(AuditListRequest request, CancellationToken cancellationToken = default);
}
