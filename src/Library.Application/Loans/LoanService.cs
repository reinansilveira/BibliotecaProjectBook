using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Events;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Members;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Library.Application.Loans;

public sealed class LoanService : ILoanService
{
    private readonly ILoanRepository _loans;
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly IMemberRepository _members;
    private readonly IReservationRepository _reservations;
    private readonly IFineRepository _fines;
    private readonly IMemberStandingService _standing;
    private readonly ReservationQueue _queue;
    private readonly IInbox _inbox;
    private readonly IEventPublisher _events;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateLoanRequest> _validator;
    private readonly FineSettings _fineSettings;
    private readonly TimeProvider _timeProvider;

    public LoanService(
        ILoanRepository loans,
        IBookRepository books,
        IBookCopyRepository copies,
        IMemberRepository members,
        IReservationRepository reservations,
        IFineRepository fines,
        IMemberStandingService standing,
        ReservationQueue queue,
        IInbox inbox,
        IEventPublisher events,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IValidator<CreateLoanRequest> validator,
        IOptions<FineSettings> fineSettings,
        TimeProvider timeProvider)
    {
        _loans = loans;
        _books = books;
        _copies = copies;
        _members = members;
        _reservations = reservations;
        _fines = fines;
        _standing = standing;
        _queue = queue;
        _inbox = inbox;
        _events = events;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _fineSettings = fineSettings.Value;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<LoanResponse> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var loanId = await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(request.BookId), token);

            var member = await GetActiveMemberAsync(request.MemberId, token);
            var book = await GetActiveBookAsync(request.BookId, token);

            await _standing.EnsureCanBorrowAsync(member.Id, token);

            if (await _loans.HasActiveLoanAsync(member.Id, book.Id, Now, token))
            {
                throw new DomainException("You are already borrowing this book.");
            }

            var reservation = await _reservations.GetOpenAsync(member.Id, book.Id, request.Format, token);
            await EnsureCanTakeAsync(book, request.Format, reservation, token);

            var loan = await CreateLoanAsync(book, member, request.Format, token);
            CloseReservation(reservation);

            await _loans.AddAsync(loan, token);
            _events.Publish(new LoanCreatedEvent(loan.Id, member.Id, member.Name, member.Email, book.Title, loan.DueDate, loan.Format == BookFormat.Ebook));
            _inbox.Add(member.Id, NotificationTypes.LoanCreated, "Loan confirmed", $"\"{book.Title}\" is yours until {loan.DescribeDueDate()}.", "/account/loans");
            await _unitOfWork.SaveChangesAsync(token);

            return loan.Id;
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return await GetByIdAsync(loanId, cancellationToken);
    }

    public async Task<LoanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var view = await _loans.GetViewAsync(id, cancellationToken) ?? throw new NotFoundException("Loan not found.");

        return view.ToResponse(Now);
    }

    public async Task<PagedResult<LoanResponse>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _loans.ListByMemberAsync(memberId, request, cancellationToken);
        var now = Now;

        return page.Map(view => view.ToResponse(now));
    }

    public async Task<LoanResponse> ReturnAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _loans.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Loan not found.");

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(existing.BookId), token);

            var loan = await _loans.GetByIdAsync(id, token) ?? throw new NotFoundException("Loan not found.");
            var member = await _members.GetByIdAsync(loan.MemberId, token) ?? throw new NotFoundException("Member not found.");
            var book = await _books.GetByIdAsync(loan.BookId, token) ?? throw new NotFoundException("Book not found.");
            var now = Now;
            var daysOverdue = loan.DaysOverdue(now);

            loan.Return(now);
            _events.Publish(new LoanReturnedEvent(loan.Id, member.Id, member.Name, member.Email, book.Title, now, daysOverdue > 0));
            _inbox.Add(member.Id, NotificationTypes.LoanReturned, "Book returned", $"We received \"{book.Title}\". Thank you!", "/account/loans");
            await IssueFineAsync(loan, member, book, daysOverdue, token);
            await _unitOfWork.SaveChangesAsync(token);
            await _queue.PromoteAsync(book, loan.Format, now, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<LoanResponse> RenewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _loans.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Loan not found.");

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(existing.BookId), token);

            var loan = await _loans.GetByIdAsync(id, token) ?? throw new NotFoundException("Loan not found.");

            if (await _reservations.HasWaitingAsync(loan.BookId, loan.Format, token))
            {
                throw new DomainException("Other members are waiting for this book, so the loan cannot be renewed.");
            }

            loan.Renew(Now);
            _inbox.Add(loan.MemberId, NotificationTypes.LoanRenewed, "Loan renewed", $"The new due date is {loan.DescribeDueDate()}.", "/account/loans");
            await _unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task EnsureCanTakeAsync(Book book, BookFormat format, Reservation? reservation, CancellationToken cancellationToken)
    {
        if (format == BookFormat.Ebook && !book.HasEbook)
        {
            throw new DomainException("This book has no e-book.");
        }

        if (format == BookFormat.Print && await _copies.CountInCirculationAsync(book.Id, cancellationToken) == 0)
        {
            throw new DomainException("This book has no printed copies.");
        }

        if (reservation?.Status == ReservationStatus.Ready)
        {
            return;
        }

        if (await _queue.FreeCapacityAsync(book, format, Now, cancellationToken) > 0)
        {
            return;
        }

        throw new DomainException(format == BookFormat.Ebook
            ? "All e-book licenses are in use. Join the waiting list."
            : "No copies are available right now. Join the waiting list.");
    }

    private async Task<Loan> CreateLoanAsync(Book book, Member member, BookFormat format, CancellationToken cancellationToken)
    {
        if (format == BookFormat.Ebook)
        {
            return Loan.ForEbook(book.Id, member.Id, Now);
        }

        var copy = await _copies.FindFreeCopyAsync(book.Id, cancellationToken)
            ?? throw new DomainException("No copies are available right now. Join the waiting list.");

        return Loan.ForPrint(book.Id, copy.Id, member.Id, Now);
    }

    private void CloseReservation(Reservation? reservation)
    {
        if (reservation is null)
        {
            return;
        }

        if (reservation.Status == ReservationStatus.Waiting)
        {
            reservation.MarkReady(Now);
        }

        reservation.Fulfill(Now);
    }

    private async Task IssueFineAsync(Loan loan, Member member, Book book, int daysOverdue, CancellationToken cancellationToken)
    {
        if (daysOverdue <= 0)
        {
            return;
        }

        var fine = new Fine(member.Id, loan.Id, daysOverdue, _fineSettings.DailyRate, Now);

        await _fines.AddAsync(fine, cancellationToken);
        _events.Publish(new FineIssuedEvent(fine.Id, member.Id, member.Name, member.Email, book.Title, daysOverdue, fine.Amount, _fineSettings.Currency));
        _inbox.Add(
            member.Id,
            NotificationTypes.FineIssued,
            "Late return fine",
            $"\"{book.Title}\" was returned {daysOverdue} day(s) late. Fine: {_fineSettings.Currency} {fine.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}.",
            "/account/fines");
    }

    private async Task<Member> GetActiveMemberAsync(Guid id, CancellationToken cancellationToken)
    {
        var member = await _members.GetByIdAsync(id, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            throw new NotFoundException("Member not found.");
        }

        return member;
    }

    private async Task<Book> GetActiveBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(id, cancellationToken);

        if (book is null || book.IsDeleted)
        {
            throw new NotFoundException("Book not found.");
        }

        return book;
    }
}
