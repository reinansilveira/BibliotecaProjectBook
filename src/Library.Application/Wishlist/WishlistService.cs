using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Wishlist;

public sealed class WishlistService : IWishlistService
{
    public const int MaxItems = 100;

    private readonly IWishlistRepository _wishlist;
    private readonly IBookRepository _books;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public WishlistService(IWishlistRepository wishlist, IBookRepository books, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _wishlist = wishlist;
        _books = books;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<BookResponse>> ListAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var ids = await _wishlist.ListBookIdsAsync(memberId, cancellationToken);
        var views = await _books.ListViewsAsync(ids, Now, cancellationToken);

        return ids.Join(views, id => id, view => view.Id, (_, view) => view).ToList();
    }

    public async Task AddAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await _books.GetByIdAsync(bookId, cancellationToken);

        if (book is null || book.IsDeleted)
        {
            throw new NotFoundException("Book not found.");
        }

        if (await _wishlist.GetAsync(memberId, bookId, cancellationToken) is not null)
        {
            return;
        }

        if ((await _wishlist.ListBookIdsAsync(memberId, cancellationToken)).Count >= MaxItems)
        {
            throw new DomainException($"A wishlist can have at most {MaxItems} books.");
        }

        await _wishlist.AddAsync(new WishlistItem(memberId, bookId, Now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default)
    {
        var item = await _wishlist.GetAsync(memberId, bookId, cancellationToken);

        if (item is null)
        {
            return;
        }

        _wishlist.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
