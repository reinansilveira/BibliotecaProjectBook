using Library.Application.Common.Pagination;
using Library.Application.Reviews;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly LibraryDbContext _context;

    public ReviewRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Review?> GetAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default) =>
        _context.Reviews.FirstOrDefaultAsync(review => review.BookId == bookId && review.MemberId == memberId, cancellationToken);

    public Task<ReviewView?> GetViewAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default) =>
        ToViews(_context.Reviews.AsNoTracking().Where(review => review.BookId == bookId && review.MemberId == memberId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<ReviewView>> ListByBookAsync(Guid bookId, PageRequest request, CancellationToken cancellationToken = default) =>
        _context.Reviews
            .AsNoTracking()
            .Where(review => review.BookId == bookId)
            .OrderByDescending(review => review.UpdatedAt)
            .ThenBy(review => review.Id)
            .ToPagedResultAsync(ToViews, request, cancellationToken);

    public Task RefreshBookRatingAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        _context.Books
            .Where(book => book.Id == bookId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(book => book.RatingsCount, book => _context.Reviews.Count(review => review.BookId == book.Id))
                    .SetProperty(
                        book => book.AverageRating,
                        book => _context.Reviews.Where(review => review.BookId == book.Id).Average(review => (decimal?)review.Rating) ?? 0m),
                cancellationToken);

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default) =>
        await _context.Reviews.AddAsync(review, cancellationToken);

    public void Remove(Review review) => _context.Reviews.Remove(review);

    private IQueryable<ReviewView> ToViews(IQueryable<Review> reviews) =>
        from review in reviews
        join member in _context.Members on review.MemberId equals member.Id
        select new ReviewView(review, member.Name);
}
