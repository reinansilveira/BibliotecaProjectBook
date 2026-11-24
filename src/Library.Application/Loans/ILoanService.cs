using Library.Application.Common.Pagination;

namespace Library.Application.Loans;

public interface ILoanService
{
    Task<LoanResponse> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);

    Task<LoanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<LoanResponse>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default);

    Task<LoanResponse> ReturnAsync(Guid id, CancellationToken cancellationToken = default);
}
