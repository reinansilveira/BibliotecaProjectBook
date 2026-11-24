using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Members;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<PagedResult<Member>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Member>> ListByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> IsEmailInUseAsync(string email, Guid? excludingMemberId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Member member, CancellationToken cancellationToken = default);
}
