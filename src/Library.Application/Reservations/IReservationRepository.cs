using Library.Application.Common.Pagination;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Reservations;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReservationView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationView>> ListByMemberAsync(Guid memberId, ReservationListRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationView>> ListQueueAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Reservation?> GetOpenAsync(Guid memberId, Guid bookId, BookFormat format, CancellationToken cancellationToken = default);

    Task<int> CountOpenByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<int> CountReadyAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default);

    Task<bool> HasWaitingAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> ListNextWaitingAsync(Guid bookId, BookFormat format, int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> ListExpiredHoldsAsync(DateTime now, CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
}
