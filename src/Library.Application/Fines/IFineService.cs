using Library.Application.Common.Pagination;

namespace Library.Application.Fines;

public interface IFineService
{
    Task<FineResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<FineResponse>> ListAsync(Guid? memberId, FineListRequest request, CancellationToken cancellationToken = default);

    Task<FineResponse> PayAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FineResponse> WaiveAsync(Guid id, CancellationToken cancellationToken = default);
}
