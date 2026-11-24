using Library.Application.Common.Pagination;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly LibraryDbContext _context;

    public ReservationRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Reservations.FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public Task<ReservationView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default) =>
        ToViews(_context.Reservations.AsNoTracking().Where(reservation => reservation.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<ReservationView>> ListByMemberAsync(Guid memberId, ReservationListRequest request, CancellationToken cancellationToken = default)
    {
        var reservations = _context.Reservations.AsNoTracking().Where(reservation => reservation.MemberId == memberId);

        if (request.OpenOnly)
        {
            reservations = reservations.Where(reservation => reservation.Status == ReservationStatus.Waiting || reservation.Status == ReservationStatus.Ready);
        }

        return reservations
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ThenBy(reservation => reservation.Id)
            .ToPagedResultAsync(ToViews, request, cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationView>> ListQueueAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        await ToViews(_context.Reservations
                .AsNoTracking()
                .Where(reservation => reservation.BookId == bookId && (reservation.Status == ReservationStatus.Waiting || reservation.Status == ReservationStatus.Ready))
                .OrderBy(reservation => reservation.Status == ReservationStatus.Waiting)
                .ThenBy(reservation => reservation.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<Reservation?> GetOpenAsync(Guid memberId, Guid bookId, BookFormat format, CancellationToken cancellationToken = default) =>
        _context.Reservations.FirstOrDefaultAsync(
            reservation => reservation.MemberId == memberId
                && reservation.BookId == bookId
                && reservation.Format == format
                && (reservation.Status == ReservationStatus.Waiting || reservation.Status == ReservationStatus.Ready),
            cancellationToken);

    public Task<int> CountOpenByMemberAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _context.Reservations.CountAsync(
            reservation => reservation.MemberId == memberId && (reservation.Status == ReservationStatus.Waiting || reservation.Status == ReservationStatus.Ready),
            cancellationToken);

    public Task<int> CountReadyAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default) =>
        _context.Reservations.CountAsync(
            reservation => reservation.BookId == bookId && reservation.Format == format && reservation.Status == ReservationStatus.Ready,
            cancellationToken);

    public Task<bool> HasWaitingAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default) =>
        _context.Reservations.AnyAsync(
            reservation => reservation.BookId == bookId && reservation.Format == format && reservation.Status == ReservationStatus.Waiting,
            cancellationToken);

    public async Task<IReadOnlyList<Reservation>> ListNextWaitingAsync(Guid bookId, BookFormat format, int count, CancellationToken cancellationToken = default) =>
        await _context.Reservations
            .Where(reservation => reservation.BookId == bookId && reservation.Format == format && reservation.Status == ReservationStatus.Waiting)
            .OrderBy(reservation => reservation.CreatedAt)
            .ThenBy(reservation => reservation.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> ListExpiredHoldsAsync(DateTime now, CancellationToken cancellationToken = default) =>
        await _context.Reservations
            .Where(reservation => reservation.Status == ReservationStatus.Ready && reservation.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default) =>
        await _context.Reservations.AddAsync(reservation, cancellationToken);

    private IQueryable<ReservationView> ToViews(IQueryable<Reservation> reservations) =>
        from reservation in reservations
        join book in _context.Books on reservation.BookId equals book.Id
        join member in _context.Members on reservation.MemberId equals member.Id
        select new ReservationView(
            reservation,
            book.Title,
            book.Author,
            book.CoverUrl,
            member.Name,
            reservation.Status == ReservationStatus.Waiting
                ? _context.Reservations.Count(other => other.BookId == reservation.BookId
                    && other.Format == reservation.Format
                    && other.Status == ReservationStatus.Waiting
                    && other.CreatedAt < reservation.CreatedAt) + 1
                : null);
}
