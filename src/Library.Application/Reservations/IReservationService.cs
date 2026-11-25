using Library.Application.Common.Pagination;

namespace Library.Application.Reservations;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> ListByMemberAsync(Guid memberId, ReservationListRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationResponse>> ListQueueAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
