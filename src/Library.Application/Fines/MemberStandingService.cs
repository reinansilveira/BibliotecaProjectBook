using Library.Application.Loans;
using Library.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Library.Application.Fines;

public sealed class MemberStandingService : IMemberStandingService
{
    private readonly ILoanRepository _loans;
    private readonly IFineRepository _fines;
    private readonly FineSettings _settings;
    private readonly TimeProvider _timeProvider;

    public MemberStandingService(ILoanRepository loans, IFineRepository fines, IOptions<FineSettings> settings, TimeProvider timeProvider)
    {
        _loans = loans;
        _fines = fines;
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public async Task<StandingResponse> GetAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeLoans = await _loans.CountActiveByMemberAsync(memberId, now, cancellationToken);
        var overdueLoans = await _loans.CountOverdueByMemberAsync(memberId, now, cancellationToken);
        var unpaid = await _fines.SumUnpaidAsync(memberId, cancellationToken);
        var reasons = new List<string>();

        if (overdueLoans > 0)
        {
            reasons.Add("Return your overdue books before borrowing again.");
        }

        if (unpaid > 0)
        {
            reasons.Add("Pay your outstanding fines before borrowing again.");
        }

        if (activeLoans >= LoanPolicy.MaxActiveLoans)
        {
            reasons.Add($"You already have {LoanPolicy.MaxActiveLoans} active loans.");
        }

        return new StandingResponse(reasons.Count == 0, activeLoans, LoanPolicy.MaxActiveLoans, overdueLoans, unpaid, _settings.Currency, reasons);
    }

    public async Task EnsureCanBorrowAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var standing = await GetAsync(memberId, cancellationToken);

        if (standing.CanBorrow)
        {
            return;
        }

        throw new DomainException(standing.Reasons[0]);
    }
}
