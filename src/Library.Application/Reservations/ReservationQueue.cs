using Library.Application.Abstractions;
using Library.Application.Books.Inventory;
using Library.Application.Events;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Reservations;

public sealed class ReservationQueue
{
    private readonly IBookCopyRepository _copies;
    private readonly ILoanRepository _loans;
    private readonly IReservationRepository _reservations;
    private readonly IMemberRepository _members;
    private readonly IInbox _inbox;
    private readonly IEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;

    public ReservationQueue(
        IBookCopyRepository copies,
        ILoanRepository loans,
        IReservationRepository reservations,
        IMemberRepository members,
        IInbox inbox,
        IEventPublisher events,
        IUnitOfWork unitOfWork)
    {
        _copies = copies;
        _loans = loans;
        _reservations = reservations;
        _members = members;
        _inbox = inbox;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public static string LockFor(Guid bookId) => $"book:{bookId:N}";

    public async Task<int> FreeCapacityAsync(Book book, BookFormat format, DateTime now, CancellationToken cancellationToken = default)
    {
        var held = await _reservations.CountReadyAsync(book.Id, format, cancellationToken);

        if (format == BookFormat.Ebook)
        {
            if (!book.HasEbook)
            {
                return 0;
            }

            return book.EbookLicenses - await _loans.CountActiveEbookLoansAsync(book.Id, now, cancellationToken) - held;
        }

        var copies = await _copies.CountInCirculationAsync(book.Id, cancellationToken);

        return copies - await _loans.CountActivePrintLoansAsync(book.Id, cancellationToken) - held;
    }

    public async Task<int> PromoteAsync(Book book, BookFormat format, DateTime now, CancellationToken cancellationToken = default)
    {
        var free = await FreeCapacityAsync(book, format, now, cancellationToken);

        if (free <= 0)
        {
            return 0;
        }

        var next = await _reservations.ListNextWaitingAsync(book.Id, format, free, cancellationToken);

        if (next.Count == 0)
        {
            return 0;
        }

        var members = (await _members.ListByIdsAsync(next.Select(reservation => reservation.MemberId).ToList(), cancellationToken))
            .ToDictionary(member => member.Id);

        foreach (var reservation in next)
        {
            reservation.MarkReady(now);
            var member = members[reservation.MemberId];

            _inbox.Add(
                member.Id,
                NotificationTypes.ReservationReady,
                "Your reserved book is available",
                $"\"{book.Title}\" is held for you for {Reservation.HoldPeriodInHours} hours.",
                $"/books/{book.Id}");
            _events.Publish(new ReservationReadyEvent(reservation.Id, member.Id, member.Name, member.Email, book.Title, reservation.ExpiresAt!.Value));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return next.Count;
    }
}
