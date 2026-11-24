using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Loans;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Loan>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> ListOverdueAsync(DateTime now, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<bool> HasOverdueLoanAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    Task<bool> IsBookOnLoanAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
}
