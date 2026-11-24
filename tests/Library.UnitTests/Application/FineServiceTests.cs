using Library.Application.Common.Exceptions;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class FineServiceTests
{
    private readonly IFineRepository _fines = Substitute.For<IFineRepository>();
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly RecordingInbox _inbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly IOptions<FineSettings> _settings = Options.Create(new FineSettings { DailyRate = 1m, Currency = "BRL" });
    private readonly FineService _service;
    private readonly MemberStandingService _standing;
    private readonly Fine _fine;
    private readonly Guid _memberId = Guid.NewGuid();

    public FineServiceTests()
    {
        _service = new FineService(_fines, _inbox, _unitOfWork, _settings, _clock);
        _standing = new MemberStandingService(_loans, _fines, _settings, _clock);
        _fine = new Fine(_memberId, Guid.NewGuid(), 2, 1m, _clock.GetUtcNow().UtcDateTime);
        _fines.GetByIdAsync(_fine.Id, Arg.Any<CancellationToken>()).Returns(_fine);
        _fines.GetViewAsync(_fine.Id, Arg.Any<CancellationToken>()).Returns(new FineView(_fine, "Ana", "Dune"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Simulated_payment_settles_the_fine_with_a_reference()
    {
        var response = await _service.PayAsync(_fine.Id, Token);

        Assert.Equal(nameof(FineStatus.Paid), response.Status);
        Assert.StartsWith("SIM-", response.PaymentReference);
        Assert.Equal("BRL", response.Currency);
        Assert.Equal(NotificationTypes.FineSettled, Assert.Single(_inbox.Notifications).Type);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Settled_fine_cannot_be_waived()
    {
        await _service.PayAsync(_fine.Id, Token);

        await Assert.ThrowsAsync<DomainException>(() => _service.WaiveAsync(_fine.Id, Token));
    }

    [Fact]
    public async Task Unknown_fine_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.PayAsync(Guid.NewGuid(), Token));
    }

    [Fact]
    public async Task Member_in_good_standing_can_borrow()
    {
        var standing = await _standing.GetAsync(_memberId, Token);

        Assert.True(standing.CanBorrow);
        Assert.Empty(standing.Reasons);
        await _standing.EnsureCanBorrowAsync(_memberId, Token);
    }

    [Fact]
    public async Task Unpaid_fines_block_new_loans()
    {
        _fines.SumUnpaidAsync(_memberId, Arg.Any<CancellationToken>()).Returns(2m);

        var standing = await _standing.GetAsync(_memberId, Token);

        Assert.False(standing.CanBorrow);
        Assert.Equal(2m, standing.UnpaidFines);
        await Assert.ThrowsAsync<DomainException>(() => _standing.EnsureCanBorrowAsync(_memberId, Token));
    }

    [Fact]
    public async Task Overdue_loans_and_the_loan_limit_are_reported()
    {
        _loans.CountOverdueByMemberAsync(_memberId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(1);
        _loans.CountActiveByMemberAsync(_memberId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(LoanPolicy.MaxActiveLoans);

        var standing = await _standing.GetAsync(_memberId, Token);

        Assert.Equal(2, standing.Reasons.Count);
        Assert.Equal(1, standing.OverdueLoans);
    }
}
