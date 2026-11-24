using Library.Application.Common.Pagination;

namespace Library.Application.Members;

public interface IMemberService
{
    Task<MemberResponse> RegisterAsync(RegisterMemberRequest request, CancellationToken cancellationToken = default);

    Task<MemberResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<MemberResponse>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default);

    Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
