using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Events;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Reservations;
using Library.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Library.Application.Maintenance;

public sealed class MaintenanceService : IMaintenanceService
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    private readonly ILoanRepository _loans;
    private readonly IReservationRepository _reservations;
    private readonly IBookRepository _books;
    private readonly ReservationQueue _queue;
    private readonly IInbox _inbox;
    private readonly IEventPublisher _events;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FineSettings _fineSettings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        ILoanRepository loans,
        IReservationRepository reservations,
        IBookRepository books,
        ReservationQueue queue,
        IInbox inbox,
        IEventPublisher events,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IOptions<FineSettings> fineSettings,
        TimeProvider timeProvider,
        ILogger<MaintenanceService> logger)
    {
        _loans = loans;
        _reservations = reservations;
        _books = books;
        _queue = queue;
        _inbox = inbox;
        _events = events;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _fineSettings = fineSettings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<MaintenanceResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var (expiredLoans, promotedAfterLoans) = await ExpireEbookLoansAsync(cancellationToken);
        var (expiredHolds, promotedAfterHolds) = await ExpireHoldsAsync(cancellationToken);
        var reminders = await SendDueSoonRemindersAsync(cancellationToken);
        var notices = await SendOverdueNoticesAsync(cancellationToken);
        var result = new MaintenanceResult(expiredLoans, expiredHolds, promotedAfterLoans + promotedAfterHolds, reminders, notices);

        if (expiredLoans + expiredHolds > 0)
        {
            await _cache.InvalidateAsync(cancellationToken);
        }

        _logger.LogInformation("Maintenance finished: {@Result}", result);

        return result;
    }

    private async Task<(int Expired, int Promoted)> ExpireEbookLoansAsync(CancellationToken cancellationToken)
    {
        var expired = await _loans.ListExpiredEbookLoansAsync(Now, cancellationToken);
        var promoted = 0;

        foreach (var group in expired.GroupBy(loan => loan.BookId))
        {
            promoted += await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(group.Key), token);

                foreach (var loan in group)
                {
                    loan.Expire();
                    _inbox.Add(loan.MemberId, NotificationTypes.EbookExpired, "E-book loan ended", "Your e-book loan ended. You can borrow it again when a license is free.", "/account/loans");
                }

                await _unitOfWork.SaveChangesAsync(token);

                var book = await _books.GetByIdAsync(group.Key, token);

                return book is null ? 0 : await _queue.PromoteAsync(book, BookFormat.Ebook, Now, token);
            }, cancellationToken);
        }

        return (expired.Count, promoted);
    }

    private async Task<(int Expired, int Promoted)> ExpireHoldsAsync(CancellationToken cancellationToken)
    {
        var holds = await _reservations.ListExpiredHoldsAsync(Now, cancellationToken);
        var promoted = 0;

        foreach (var group in holds.GroupBy(hold => (hold.BookId, hold.Format)))
        {
            promoted += await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(group.Key.BookId), token);

                foreach (var hold in group.Where(hold => hold.TryExpire(Now)))
                {
                    _inbox.Add(hold.MemberId, NotificationTypes.ReservationExpired, "Reservation expired", "You did not pick up your reserved book in time, so it went to the next member.", "/account/reservations");
                }

                await _unitOfWork.SaveChangesAsync(token);

                var book = await _books.GetByIdAsync(group.Key.BookId, token);

                return book is null ? 0 : await _queue.PromoteAsync(book, group.Key.Format, Now, token);
            }, cancellationToken);
        }

        return (holds.Count, promoted);
    }

    private async Task<int> SendDueSoonRemindersAsync(CancellationToken cancellationToken)
    {
        var dueSoon = await _loans.ListDueSoonWithoutReminderAsync(Now, Now.Add(ReminderWindow), cancellationToken);

        foreach (var view in dueSoon)
        {
            var loan = await _loans.GetByIdAsync(view.Loan.Id, cancellationToken);
            loan?.MarkReminderSent(Now);

            var isEbook = view.Loan.Format == BookFormat.Ebook;
            _events.Publish(new LoanDueSoonEvent(view.Loan.Id, view.Loan.MemberId, view.MemberName, view.MemberEmail, view.BookTitle, view.Loan.DueDate, isEbook));
            _inbox.Add(view.Loan.MemberId, NotificationTypes.LoanDueSoon, isEbook ? "E-book loan ends soon" : "Loan due soon", $"\"{view.BookTitle}\" is due on {view.Loan.DescribeDueDate()}.", "/account/loans");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dueSoon.Count;
    }

    private async Task<int> SendOverdueNoticesAsync(CancellationToken cancellationToken)
    {
        var overdue = await _loans.ListOverdueWithoutNoticeAsync(Now, cancellationToken);

        foreach (var view in overdue)
        {
            var loan = await _loans.GetByIdAsync(view.Loan.Id, cancellationToken);
            loan?.MarkOverdueNoticeSent(Now);

            _events.Publish(new LoanOverdueEvent(view.Loan.Id, view.Loan.MemberId, view.MemberName, view.MemberEmail, view.BookTitle, view.Loan.DueDate, _fineSettings.DailyRate, _fineSettings.Currency));
            _inbox.Add(view.Loan.MemberId, NotificationTypes.LoanOverdue, "Loan overdue", $"\"{view.BookTitle}\" is overdue. A daily fine applies until it is returned.", "/account/loans");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return overdue.Count;
    }
}
