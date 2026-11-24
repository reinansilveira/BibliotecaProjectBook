using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Blacklist;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Events;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class LoanServiceTests
{
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IBlacklistRepository _blacklist = Substitute.For<IBlacklistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RecordingEventPublisher _events = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly LoanService _service;
    private readonly Book _book = TestData.Book();
    private readonly Member _member = TestData.Member();

    public LoanServiceTests()
    {
        _service = new LoanService(_loans, _books, _members, _blacklist, _unitOfWork, _events, new CreateLoanRequestValidator(), _clock);

        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
        _members.GetByIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(_member);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private CreateLoanRequest Request => new(_book.Id, _member.Id);

    [Fact]
    public async Task Creates_the_loan_when_every_rule_is_met()
    {
        var response = await _service.CreateAsync(Request, Token);

        Assert.Equal(LoanStatus.Active, response.Status);
        Assert.Equal(Now.AddDays(Loan.LoanPeriodInDays), response.DueDate);
        await _loans.Received(1).AddAsync(Arg.Is<Loan>(loan => loan.BookId == _book.Id && loan.MemberId == _member.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publishes_a_loan_created_event()
    {
        await _service.CreateAsync(Request, Token);

        var created = Assert.IsType<LoanCreatedEvent>(Assert.Single(_events.Events));
        Assert.Equal(_member.Email, created.MemberEmail);
        Assert.Equal(_book.Title, created.BookTitle);
    }

    [Fact]
    public async Task Blacklisted_member_cannot_borrow()
    {
        _blacklist.IsBlacklistedAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Member_with_an_active_loan_cannot_borrow_again()
    {
        _loans.HasActiveLoanAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));

        Assert.Equal("The member already has an active loan.", exception.Message);
    }

    [Fact]
    public async Task Book_already_on_loan_cannot_be_borrowed()
    {
        _loans.IsBookOnLoanAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(Request, Token));

        Assert.Equal("The book is already on loan.", exception.Message);
    }

    [Fact]
    public async Task Unknown_book_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateLoanRequest(Guid.NewGuid(), _member.Id), Token));
    }

    [Fact]
    public async Task Deleted_book_returns_not_found()
    {
        _book.Delete();

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(Request, Token));
    }

    [Fact]
    public async Task Unknown_member_returns_not_found()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(new CreateLoanRequest(_book.Id, Guid.NewGuid()), Token));

        Assert.Equal("Member not found.", exception.Message);
    }

    [Fact]
    public async Task Empty_ids_fail_validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new CreateLoanRequest(Guid.Empty, Guid.Empty), Token));
    }

    [Fact]
    public async Task Returning_records_the_date_and_publishes_an_event()
    {
        var loan = new Loan(_book.Id, _member.Id, Now);
        _loans.GetByIdAsync(loan.Id, Arg.Any<CancellationToken>()).Returns(loan);
        _clock.Advance(TimeSpan.FromDays(2));

        var response = await _service.ReturnAsync(loan.Id, Token);

        Assert.Equal(LoanStatus.Returned, response.Status);
        Assert.Equal(Now, response.ReturnedAt);
        var returned = Assert.IsType<LoanReturnedEvent>(Assert.Single(_events.Events));
        Assert.False(returned.WasOverdue);
    }

    [Fact]
    public async Task Late_return_is_flagged_as_overdue_in_the_event()
    {
        var loan = new Loan(_book.Id, _member.Id, Now);
        _loans.GetByIdAsync(loan.Id, Arg.Any<CancellationToken>()).Returns(loan);
        _clock.Advance(TimeSpan.FromDays(10));

        await _service.ReturnAsync(loan.Id, Token);

        var returned = Assert.IsType<LoanReturnedEvent>(Assert.Single(_events.Events));
        Assert.True(returned.WasOverdue);
    }

    [Fact]
    public async Task Loan_past_the_due_date_is_reported_as_overdue()
    {
        var loan = new Loan(_book.Id, _member.Id, Now);
        _loans.GetByIdAsync(loan.Id, Arg.Any<CancellationToken>()).Returns(loan);
        _clock.Advance(TimeSpan.FromDays(10));

        var response = await _service.GetByIdAsync(loan.Id, Token);

        Assert.Equal(LoanStatus.Overdue, response.Status);
    }

    [Fact]
    public async Task Unknown_loan_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), Token));
    }
}
