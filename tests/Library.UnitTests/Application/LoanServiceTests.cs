using FluentValidation;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Events;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Library.UnitTests.Application;

public class LoanServiceTests
{
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IBookCopyRepository _copies = Substitute.For<IBookCopyRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IFineRepository _fines = Substitute.For<IFineRepository>();
    private readonly IMemberStandingService _standing = Substitute.For<IMemberStandingService>();
    private readonly RecordingInbox _inbox = new();
    private readonly RecordingEventPublisher _events = new();
    private readonly PassThroughCatalogCache _cache = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly LoanService _service;
    private readonly Book _book = TestData.Book();
    private readonly Member _member = TestData.Member();
    private readonly BookCopy _copy;
    private Loan? _stored;

    public LoanServiceTests()
    {
        _copy = new BookCopy(_book.Id, "CP-1", Now);
        var queue = new ReservationQueue(_copies, _loans, _reservations, _members, _inbox, _events, _unitOfWork);

        _service = new LoanService(
            _loans, _books, _copies, _members, _reservations, _fines, _standing, queue, _inbox, _events, _cache, _unitOfWork,
            new CreateLoanRequestValidator(), Options.Create(new FineSettings { DailyRate = 0.50m, Currency = "USD" }), _clock);

        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
        _members.GetByIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(_member);
        _copies.CountInCirculationAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(1);
        _copies.FindFreeCopyAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_copy);
        _loans.When(loans => loans.AddAsync(Arg.Any<Loan>(), Arg.Any<CancellationToken>())).Do(call => _stored = call.Arg<Loan>());
        _loans.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call => _stored?.Id == call.Arg<Guid>() ? _stored : null);
        _loans.GetViewAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => _stored?.Id == call.Arg<Guid>() ? new LoanView(_stored, _book.Title, _book.Author, null, _member.Name, _member.Email) : null);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private CreateLoanRequest Request(BookFormat format = BookFormat.Print) => new(_book.Id, _member.Id, format);

    [Fact]
    public async Task Borrows_a_free_printed_copy_inside_a_book_lock()
    {
        var response = await _service.CreateAsync(Request(), Token);

        Assert.Equal(LoanStatus.Active, response.Status);
        Assert.Equal(Now.AddDays(Loan.PrintLoanPeriodInDays), response.DueDate);
        Assert.Equal(_copy.Id, _stored!.CopyId);
        Assert.Equal(ReservationQueue.LockFor(_book.Id), Assert.Single(_unitOfWork.Locks));
        Assert.IsType<LoanCreatedEvent>(Assert.Single(_events.Events));
        Assert.Equal(NotificationTypes.LoanCreated, Assert.Single(_inbox.Notifications).Type);
        Assert.Equal(1, _cache.Invalidations);
    }

    [Fact]
    public async Task Borrows_an_ebook_when_a_license_is_free()
    {
        var ebook = TestData.EbookBook(licenses: 2);
        _books.GetByIdAsync(ebook.Id, Arg.Any<CancellationToken>()).Returns(ebook);
        _loans.CountActiveEbookLoansAsync(ebook.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(1);

        var response = await _service.CreateAsync(new CreateLoanRequest(ebook.Id, _member.Id, BookFormat.Ebook), Token);

        Assert.Equal("Ebook", response.Format);
        Assert.Null(_stored!.CopyId);
        Assert.Equal(Now.AddDays(Loan.EbookLoanPeriodInDays), response.DueDate);
    }

    [Fact]
    public async Task Ebook_loan_needs_an_ebook()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request(BookFormat.Ebook), Token));

        Assert.Equal("This book has no e-book.", exception.Message);
    }

    [Fact]
    public async Task Without_free_copies_the_member_is_sent_to_the_waiting_list()
    {
        _loans.CountActivePrintLoansAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(1);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request(), Token));

        Assert.Equal("No copies are available right now. Join the waiting list.", exception.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Copies_held_for_other_members_are_not_free()
    {
        _reservations.CountReadyAsync(_book.Id, BookFormat.Print, Arg.Any<CancellationToken>()).Returns(1);

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request(), Token));
    }

    [Fact]
    public async Task Ready_reservation_lets_the_member_take_the_held_copy()
    {
        var reservation = new Reservation(_book.Id, _member.Id, BookFormat.Print, Now.AddDays(-3));
        reservation.MarkReady(Now.AddHours(-1));
        _reservations.GetOpenAsync(_member.Id, _book.Id, BookFormat.Print, Arg.Any<CancellationToken>()).Returns(reservation);
        _reservations.CountReadyAsync(_book.Id, BookFormat.Print, Arg.Any<CancellationToken>()).Returns(1);

        await _service.CreateAsync(Request(), Token);

        Assert.Equal(ReservationStatus.Fulfilled, reservation.Status);
    }

    [Fact]
    public async Task Member_with_a_problem_on_the_account_cannot_borrow()
    {
        _standing.EnsureCanBorrowAsync(_member.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DomainException("Pay your outstanding fines before borrowing again."));

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request(), Token));
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Member_cannot_borrow_the_same_book_twice()
    {
        _loans.HasActiveLoanAsync(_member.Id, _book.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request(), Token));

        Assert.Equal("You are already borrowing this book.", exception.Message);
    }

    [Fact]
    public async Task Unknown_book_or_member_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateLoanRequest(Guid.NewGuid(), _member.Id), Token));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateLoanRequest(_book.Id, Guid.NewGuid()), Token));
    }

    [Fact]
    public async Task Empty_ids_fail_validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new CreateLoanRequest(Guid.Empty, Guid.Empty), Token));
    }

    [Fact]
    public async Task On_time_return_issues_no_fine()
    {
        await _service.CreateAsync(Request(), Token);
        _events.Events.Clear();
        _clock.Advance(TimeSpan.FromDays(2));

        var response = await _service.ReturnAsync(_stored!.Id, Token);

        Assert.Equal(LoanStatus.Returned, response.Status);
        Assert.False(Assert.IsType<LoanReturnedEvent>(Assert.Single(_events.Events)).WasOverdue);
        await _fines.DidNotReceive().AddAsync(Arg.Any<Fine>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Late_return_issues_a_fine_per_day()
    {
        await _service.CreateAsync(Request(), Token);
        _events.Events.Clear();
        _clock.Advance(TimeSpan.FromDays(10));

        await _service.ReturnAsync(_stored!.Id, Token);

        await _fines.Received(1).AddAsync(Arg.Is<Fine>(fine => fine.DaysOverdue == 3 && fine.Amount == 1.50m), Arg.Any<CancellationToken>());
        var fineIssued = Assert.Single(_events.Events.OfType<FineIssuedEvent>());
        Assert.Equal("USD", fineIssued.Currency);
        Assert.Contains(_inbox.Notifications, notification => notification.Type == NotificationTypes.FineIssued);
    }

    [Fact]
    public async Task Return_gives_the_copy_to_the_first_member_waiting()
    {
        await _service.CreateAsync(Request(), Token);
        var waiting = new Reservation(_book.Id, Guid.NewGuid(), BookFormat.Print, Now);
        var nextMember = new Member("Bia Souza", new DateTime(1990, 1, 1), "555", "bia@email.com", "hash", Now);
        typeof(Reservation).GetProperty(nameof(Reservation.MemberId))!.SetValue(waiting, nextMember.Id);
        _reservations.ListNextWaitingAsync(_book.Id, BookFormat.Print, 1, Arg.Any<CancellationToken>()).Returns([waiting]);
        _members.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([nextMember]);

        await _service.ReturnAsync(_stored!.Id, Token);

        Assert.Equal(ReservationStatus.Ready, waiting.Status);
        Assert.Single(_events.Events.OfType<ReservationReadyEvent>());
        Assert.Contains(_inbox.Notifications, notification => notification.MemberId == nextMember.Id && notification.Type == NotificationTypes.ReservationReady);
    }

    [Fact]
    public async Task Renewal_is_blocked_when_someone_is_waiting()
    {
        await _service.CreateAsync(Request(), Token);
        _reservations.HasWaitingAsync(_book.Id, BookFormat.Print, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.RenewAsync(_stored!.Id, Token));
        Assert.Equal(0, _stored!.RenewalCount);
    }

    [Fact]
    public async Task Renewal_extends_the_due_date()
    {
        await _service.CreateAsync(Request(), Token);

        var response = await _service.RenewAsync(_stored!.Id, Token);

        Assert.Equal(Now.AddDays(14), response.DueDate);
        Assert.Equal(0, response.RenewalsLeft);
    }

    [Fact]
    public async Task Unknown_loan_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), Token));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.ReturnAsync(Guid.NewGuid(), Token));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RenewAsync(Guid.NewGuid(), Token));
    }
}
