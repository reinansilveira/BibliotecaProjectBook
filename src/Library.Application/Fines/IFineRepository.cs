using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Fines;

public interface IFineRepository
{
    Task<Fine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FineView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<FineView>> ListAsync(Guid? memberId, FineListRequest request, CancellationToken cancellationToken = default);

    Task<decimal> SumUnpaidAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task AddAsync(Fine fine, CancellationToken cancellationToken = default);
}
