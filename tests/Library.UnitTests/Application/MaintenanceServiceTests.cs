using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Events;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Maintenance;
using Library.Application.Members;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class MaintenanceServiceTests
{
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly RecordingInbox _inbox = new();
    private readonly RecordingEventPublisher _events = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly MaintenanceService _service;

    public MaintenanceServiceTests()
    {
        var queue = new ReservationQueue(Substitute.For<IBookCopyRepository>(), _loans, _reservations, _members, _inbox, _events, _unitOfWork);
        _service = new MaintenanceService(
            _loans, _reservations, _books, queue, _inbox, _events, new PassThroughCatalogCache(), _unitOfWork,
            Options.Create(new FineSettings()), _clock, NullLogger<MaintenanceService>.Instance);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    [Fact]
    public async Task Expired_ebook_loans_end_and_free_the_license()
    {
        var book = TestData.EbookBook(licenses: 1);
        var loan = Loan.ForEbook(book.Id, Guid.NewGuid(), Now.AddDays(-15));
        _loans.ListExpiredEbookLoansAsync(Now, Arg.Any<CancellationToken>()).Returns([loan]);
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        var result = await _service.RunAsync(Token);

        Assert.Equal(1, result.ExpiredEbookLoans);
        Assert.True(loan.IsReturned);
        Assert.Contains(_inbox.Notifications, notification => notification.Type == NotificationTypes.EbookExpired);
    }

    [Fact]
    public async Task Holds_not_picked_up_expire()
    {
        var hold = new Reservation(Guid.NewGuid(), Guid.NewGuid(), BookFormat.Print, Now.AddDays(-5));
        hold.MarkReady(Now.AddDays(-3));
        _reservations.ListExpiredHoldsAsync(Now, Arg.Any<CancellationToken>()).Returns([hold]);

        var result = await _service.RunAsync(Token);

        Assert.Equal(1, result.ExpiredHolds);
        Assert.Equal(ReservationStatus.Expired, hold.Status);
    }

    [Fact]
    public async Task Due_soon_and_overdue_loans_are_notified_once()
    {
        var dueSoon = Loan.ForPrint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-6.5));
        var overdue = Loan.ForPrint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-9));
        _loans.ListDueSoonWithoutReminderAsync(Now, Now.AddHours(24), Arg.Any<CancellationToken>())
            .Returns([new LoanView(dueSoon, "Dune", "Herbert", null, "Ana", "ana@email.com")]);
        _loans.ListOverdueWithoutNoticeAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new LoanView(overdue, "Emma", "Austen", null, "Bia", "bia@email.com")]);
        _loans.GetByIdAsync(dueSoon.Id, Arg.Any<CancellationToken>()).Returns(dueSoon);
        _loans.GetByIdAsync(overdue.Id, Arg.Any<CancellationToken>()).Returns(overdue);

        var result = await _service.RunAsync(Token);

        Assert.Equal(1, result.DueSoonReminders);
        Assert.Equal(1, result.OverdueNotices);
        Assert.NotNull(dueSoon.ReminderSentAt);
        Assert.NotNull(overdue.OverdueNoticeSentAt);
        Assert.Single(_events.Events.OfType<LoanDueSoonEvent>());
        Assert.Single(_events.Events.OfType<LoanOverdueEvent>());
    }
}
