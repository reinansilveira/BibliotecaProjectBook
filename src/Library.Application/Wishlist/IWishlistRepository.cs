using Library.Domain.Entities;

namespace Library.Application.Wishlist;

public interface IWishlistRepository
{
    Task<WishlistItem?> GetAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListBookIdsAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task AddAsync(WishlistItem item, CancellationToken cancellationToken = default);

    void Remove(WishlistItem item);
}
