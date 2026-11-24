using FluentValidation;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Loans;
using Library.Application.Reviews;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class ReviewServiceTests
{
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly PassThroughCatalogCache _cache = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ReviewService _service;
    private readonly Book _book = TestData.Book();
    private readonly Guid _memberId = Guid.NewGuid();

    public ReviewServiceTests()
    {
        _service = new ReviewService(_reviews, _books, _loans, _cache, _unitOfWork, new ReviewRequestValidator(), new FakeTimeProvider());
        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
        _loans.HasBorrowedAsync(_memberId, _book.Id, Arg.Any<CancellationToken>()).Returns(true);
        _reviews.GetViewAsync(_book.Id, _memberId, Arg.Any<CancellationToken>())
            .Returns(new ReviewView(new Review(_book.Id, _memberId, 5, "Great", DateTime.UtcNow), "Maria Clara Souza"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_review_is_added_and_the_rating_is_recalculated()
    {
        var response = await _service.SaveAsync(_book.Id, _memberId, new ReviewRequest(5, "Great"), Token);

        await _reviews.Received(1).AddAsync(Arg.Is<Review>(review => review.Rating == 5), Arg.Any<CancellationToken>());
        await _reviews.Received(1).RefreshBookRatingAsync(_book.Id, Arg.Any<CancellationToken>());
        Assert.Equal("Maria S.", response.ReviewerName);
        Assert.Equal(1, _cache.Invalidations);
    }

    [Fact]
    public async Task Second_save_updates_the_existing_review()
    {
        var existing = new Review(_book.Id, _memberId, 2, "Meh", DateTime.UtcNow);
        _reviews.GetAsync(_book.Id, _memberId, Arg.Any<CancellationToken>()).Returns(existing);

        await _service.SaveAsync(_book.Id, _memberId, new ReviewRequest(4, "Better on a second read"), Token);

        Assert.Equal(4, existing.Rating);
        await _reviews.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Only_readers_can_review()
    {
        _loans.HasBorrowedAsync(_memberId, _book.Id, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<DomainException>(() => _service.SaveAsync(_book.Id, _memberId, new ReviewRequest(5), Token));
    }

    [Fact]
    public async Task Invalid_rating_fails_validation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.SaveAsync(_book.Id, _memberId, new ReviewRequest(9), Token));
    }

    [Fact]
    public async Task Deleting_a_missing_review_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(_book.Id, _memberId, Token));
    }

    [Fact]
    public async Task Reviews_of_an_unknown_book_return_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.ListByBookAsync(Guid.NewGuid(), new(), Token));
    }
}
