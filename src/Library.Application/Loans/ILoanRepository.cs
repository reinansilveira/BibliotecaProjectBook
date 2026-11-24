using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Loans;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LoanView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<LoanView>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default);

    Task<int> CountActiveByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    Task<int> CountOverdueByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanAsync(Guid memberId, Guid bookId, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanForBookAsync(Guid bookId, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> HasBorrowedAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default);

    Task<int> CountActivePrintLoansAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<int> CountActiveEbookLoansAsync(Guid bookId, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> ListExpiredEbookLoansAsync(DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoanView>> ListDueSoonWithoutReminderAsync(DateTime now, DateTime until, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoanView>> ListOverdueWithoutNoticeAsync(DateTime now, CancellationToken cancellationToken = default);

    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
}
