using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IBookRepository _books;
    private readonly ILoanRepository _loans;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReviewRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public ReviewService(
        IReviewRepository reviews,
        IBookRepository books,
        ILoanRepository loans,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IValidator<ReviewRequest> validator,
        TimeProvider timeProvider)
    {
        _reviews = reviews;
        _books = books;
        _loans = loans;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<ReviewResponse>> ListByBookAsync(Guid bookId, PageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);
        await GetActiveBookAsync(bookId, cancellationToken);

        var page = await _reviews.ListByBookAsync(bookId, request, cancellationToken);

        return page.Map(view => view.ToResponse());
    }

    public async Task<ReviewResponse?> GetMineAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default) =>
        (await _reviews.GetViewAsync(bookId, memberId, cancellationToken))?.ToResponse();

    public async Task<ReviewResponse> SaveAsync(Guid bookId, Guid memberId, ReviewRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await GetActiveBookAsync(bookId, cancellationToken);

        if (!await _loans.HasBorrowedAsync(memberId, bookId, cancellationToken))
        {
            throw new DomainException("Only members who borrowed this book can review it.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var review = await _reviews.GetAsync(bookId, memberId, token);

            if (review is null)
            {
                await _reviews.AddAsync(new Review(bookId, memberId, request.Rating, request.Comment, now), token);
            }

            review?.Change(request.Rating, request.Comment, now);
            await _unitOfWork.SaveChangesAsync(token);
            await _reviews.RefreshBookRatingAsync(bookId, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return (await GetMineAsync(bookId, memberId, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid bookId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var review = await _reviews.GetAsync(bookId, memberId, cancellationToken) ?? throw new NotFoundException("Review not found.");

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            _reviews.Remove(review);
            await _unitOfWork.SaveChangesAsync(token);
            await _reviews.RefreshBookRatingAsync(bookId, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);
    }

    private async Task GetActiveBookAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(bookId, cancellationToken);

        if (book is not null && !book.IsDeleted)
        {
            return;
        }

        throw new NotFoundException("Book not found.");
    }
}
