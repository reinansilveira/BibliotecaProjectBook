using Library.Application.Wishlist;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class WishlistRepository : IWishlistRepository
{
    private readonly LibraryDbContext _context;

    public WishlistRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<WishlistItem?> GetAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        _context.WishlistItems.FirstOrDefaultAsync(item => item.MemberId == memberId && item.BookId == bookId, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListBookIdsAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await (from item in _context.WishlistItems.AsNoTracking()
               join book in _context.Books on item.BookId equals book.Id
               where item.MemberId == memberId && !book.IsDeleted
               orderby item.AddedAt descending
               select item.BookId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WishlistItem item, CancellationToken cancellationToken = default) =>
        await _context.WishlistItems.AddAsync(item, cancellationToken);

    public void Remove(WishlistItem item) => _context.WishlistItems.Remove(item);
}
