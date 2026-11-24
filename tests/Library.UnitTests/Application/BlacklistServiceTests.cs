using Library.Application.Abstractions;
using Library.Application.Blacklist;
using Library.Application.Common.Exceptions;
using Library.Application.Events;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class BlacklistServiceTests
{
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IBlacklistRepository _blacklist = Substitute.For<IBlacklistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RecordingEventPublisher _events = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero));
    private readonly BlacklistService _service;
    private readonly Member _member = TestData.Member();

    public BlacklistServiceTests()
    {
        _service = new BlacklistService(_loans, _members, _blacklist, _unitOfWork, _events, _clock, NullLogger<BlacklistService>.Instance);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private void GivenAnOverdueLoan()
    {
        var overdue = new Loan(Guid.NewGuid(), _member.Id, Now.AddDays(-10));
        _loans.ListOverdueAsync(Now, Arg.Any<CancellationToken>()).Returns([overdue]);
        _members.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([_member]);
    }

    [Fact]
    public async Task Adds_members_with_overdue_loans_and_publishes_an_event()
    {
        GivenAnOverdueLoan();

        var added = await _service.RefreshAsync(Token);

        Assert.Equal(1, added);
        await _blacklist.Received(1).AddAsync(Arg.Is<BlacklistEntry>(entry => entry.MemberId == _member.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        var blacklisted = Assert.IsType<MemberBlacklistedEvent>(Assert.Single(_events.Events));
        Assert.Equal(_member.Email, blacklisted.MemberEmail);
    }

    [Fact]
    public async Task Does_not_add_a_member_twice()
    {
        GivenAnOverdueLoan();
        _blacklist.IsBlacklistedAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(true);

        var added = await _service.RefreshAsync(Token);

        Assert.Equal(0, added);
        await _blacklist.DidNotReceive().AddAsync(Arg.Any<BlacklistEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Nothing_changes_without_overdue_loans()
    {
        _loans.ListOverdueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _members.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var added = await _service.RefreshAsync(Token);

        Assert.Equal(0, added);
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Member_with_an_overdue_loan_cannot_be_removed()
    {
        _blacklist.GetByMemberIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(new BlacklistEntry(_member.Id, _member.Name, Now));
        _loans.HasOverdueLoanAsync(_member.Id, Now, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.RemoveAsync(_member.Id, Token));
        _blacklist.DidNotReceive().Remove(Arg.Any<BlacklistEntry>());
    }

    [Fact]
    public async Task Member_is_removed_after_returning_the_book()
    {
        var entry = new BlacklistEntry(_member.Id, _member.Name, Now);
        _blacklist.GetByMemberIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(entry);

        await _service.RemoveAsync(_member.Id, Token);

        _blacklist.Received(1).Remove(entry);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Removing_a_member_not_on_the_blacklist_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveAsync(Guid.NewGuid(), Token));
    }
}
