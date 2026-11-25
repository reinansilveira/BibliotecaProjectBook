using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Fines;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Application.Reservations;

public sealed class ReservationService : IReservationService
{
    public const int MaxOpenReservations = 5;

    private readonly IReservationRepository _reservations;
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IMemberStandingService _standing;
    private readonly ReservationQueue _queue;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReservationRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public ReservationService(
        IReservationRepository reservations,
        IBookRepository books,
        IBookCopyRepository copies,
        IMemberRepository members,
        ILoanRepository loans,
        IMemberStandingService standing,
        ReservationQueue queue,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IValidator<ReservationRequest> validator,
        TimeProvider timeProvider)
    {
        _reservations = reservations;
        _books = books;
        _copies = copies;
        _members = members;
        _loans = loans;
        _standing = standing;
        _queue = queue;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<ReservationResponse> CreateAsync(ReservationRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var reservationId = await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(request.BookId), token);

            var member = await _members.GetByIdAsync(request.MemberId, token);

            if (member is null || member.IsDeleted)
            {
                throw new NotFoundException("Member not found.");
            }

            var book = await _books.GetByIdAsync(request.BookId, token);

            if (book is null || book.IsDeleted)
            {
                throw new NotFoundException("Book not found.");
            }

            await _standing.EnsureCanBorrowAsync(member.Id, token);
            await EnsureFormatIsOfferedAsync(book, request.Format, token);

            if (await _loans.HasActiveLoanAsync(member.Id, book.Id, Now, token))
            {
                throw new DomainException("You are already borrowing this book.");
            }

            if (await _reservations.GetOpenAsync(member.Id, book.Id, request.Format, token) is not null)
            {
                throw new ConflictException("You already have a reservation for this book.");
            }

            if (await _reservations.CountOpenByMemberAsync(member.Id, token) >= MaxOpenReservations)
            {
                throw new DomainException($"A member can have at most {MaxOpenReservations} open reservations.");
            }

            if (await _queue.FreeCapacityAsync(book, request.Format, Now, token) > 0)
            {
                throw new DomainException("This book is available right now. Borrow it instead of reserving it.");
            }

            var reservation = new Reservation(book.Id, member.Id, request.Format, Now);
            await _reservations.AddAsync(reservation, token);
            await _unitOfWork.SaveChangesAsync(token);

            return reservation.Id;
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return await GetByIdAsync(reservationId, cancellationToken);
    }

    public async Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var view = await _reservations.GetViewAsync(id, cancellationToken) ?? throw new NotFoundException("Reservation not found.");

        return view.ToResponse();
    }

    public async Task<PagedResult<ReservationResponse>> ListByMemberAsync(Guid memberId, ReservationListRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _reservations.ListByMemberAsync(memberId, request, cancellationToken);

        return page.Map(view => view.ToResponse());
    }

    public async Task<IReadOnlyList<ReservationResponse>> ListQueueAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        (await _reservations.ListQueueAsync(bookId, cancellationToken)).Select(view => view.ToResponse()).ToList();

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _reservations.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Reservation not found.");

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(existing.BookId), token);

            var reservation = await _reservations.GetByIdAsync(id, token) ?? throw new NotFoundException("Reservation not found.");
            var book = await _books.GetByIdAsync(reservation.BookId, token) ?? throw new NotFoundException("Book not found.");
            var wasHolding = reservation.Status == ReservationStatus.Ready;

            reservation.Cancel(Now);
            await _unitOfWork.SaveChangesAsync(token);

            if (!wasHolding)
            {
                return;
            }

            await _queue.PromoteAsync(book, reservation.Format, Now, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);
    }

    private async Task EnsureFormatIsOfferedAsync(Book book, BookFormat format, CancellationToken cancellationToken)
    {
        if (format == BookFormat.Ebook && !book.HasEbook)
        {
            throw new DomainException("This book has no e-book.");
        }

        if (format != BookFormat.Print || await _copies.CountInCirculationAsync(book.Id, cancellationToken) > 0)
        {
            return;
        }

        throw new DomainException("This book has no printed copies.");
    }
}
