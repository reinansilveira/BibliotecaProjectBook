using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Fines;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class ReservationServiceTests
{
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IBookCopyRepository _copies = Substitute.For<IBookCopyRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IMemberStandingService _standing = Substitute.For<IMemberStandingService>();
    private readonly RecordingInbox _inbox = new();
    private readonly RecordingEventPublisher _events = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly ReservationService _service;
    private readonly Book _book = TestData.Book();
    private readonly Member _member = TestData.Member();

    public ReservationServiceTests()
    {
        var queue = new ReservationQueue(_copies, _loans, _reservations, _members, _inbox, _events, _unitOfWork);
        _service = new ReservationService(
            _reservations, _books, _copies, _members, _loans, _standing, queue, new PassThroughCatalogCache(), _unitOfWork,
            new ReservationRequestValidator(), _clock);

        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
        _members.GetByIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(_member);
        _copies.CountInCirculationAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(1);
        _loans.CountActivePrintLoansAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(1);
        _reservations.GetViewAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => new ReservationView(new Reservation(_book.Id, _member.Id, BookFormat.Print, Now), _book.Title, _book.Author, null, _member.Name, 1));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private ReservationRequest Request => new(_book.Id, _member.Id);

    [Fact]
    public async Task Joins_the_waiting_list_when_every_copy_is_taken()
    {
        var response = await _service.CreateAsync(Request, Token);

        await _reservations.Received(1).AddAsync(Arg.Is<Reservation>(reservation => reservation.MemberId == _member.Id), Arg.Any<CancellationToken>());
        Assert.Equal(1, response.Position);
    }

    [Fact]
    public async Task Available_book_should_be_borrowed_instead()
    {
        _loans.CountActivePrintLoansAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(0);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));

        Assert.Contains("Borrow it instead", exception.Message);
    }

    [Fact]
    public async Task Member_cannot_reserve_the_same_book_twice()
    {
        _reservations.GetOpenAsync(_member.Id, _book.Id, BookFormat.Print, Arg.Any<CancellationToken>())
            .Returns(new Reservation(_book.Id, _member.Id, BookFormat.Print, Now));

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(Request, Token));
    }

    [Fact]
    public async Task Member_borrowing_the_book_cannot_reserve_it()
    {
        _loans.HasActiveLoanAsync(_member.Id, _book.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));
    }

    [Fact]
    public async Task Open_reservations_are_limited()
    {
        _reservations.CountOpenByMemberAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(ReservationService.MaxOpenReservations);

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));
    }

    [Fact]
    public async Task Ebook_reservation_needs_an_ebook()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request with { Format = BookFormat.Ebook }, Token));

        Assert.Equal("This book has no e-book.", exception.Message);
    }

    [Fact]
    public async Task Book_without_printed_copies_cannot_be_reserved_in_print()
    {
        _copies.CountInCirculationAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(0);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));

        Assert.Equal("This book has no printed copies.", exception.Message);
    }

    [Fact]
    public async Task Giving_up_a_held_copy_passes_it_to_the_next_member()
    {
        var held = new Reservation(_book.Id, _member.Id, BookFormat.Print, Now);
        held.MarkReady(Now);
        var next = new Reservation(_book.Id, _member.Id, BookFormat.Print, Now.AddMinutes(1));
        _reservations.GetByIdAsync(held.Id, Arg.Any<CancellationToken>()).Returns(held);
        _loans.CountActivePrintLoansAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(0);
        _reservations.ListNextWaitingAsync(_book.Id, BookFormat.Print, 1, Arg.Any<CancellationToken>()).Returns([next]);
        _members.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([_member]);

        await _service.CancelAsync(held.Id, Token);

        Assert.Equal(ReservationStatus.Cancelled, held.Status);
        Assert.Equal(ReservationStatus.Ready, next.Status);
    }

    [Fact]
    public async Task Unknown_reservation_returns_not_found()
    {
        _reservations.GetViewAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ReservationView?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), Token));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelAsync(Guid.NewGuid(), Token));
    }
}
