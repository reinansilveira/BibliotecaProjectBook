using Library.Application.Books;

namespace Library.Application.Wishlist;

public interface IWishlistService
{
    Task<IReadOnlyList<BookResponse>> ListAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task AddAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default);
}
