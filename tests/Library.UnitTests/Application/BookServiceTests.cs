using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Application.Reports;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class BookServiceTests
{
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IBookCopyRepository _copies = Substitute.For<IBookCopyRepository>();
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly PassThroughCatalogCache _cache = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly BookService _service;

    public BookServiceTests()
    {
        _service = new BookService(
            _books, _copies, _loans, _reports, _cache, _unitOfWork,
            new CreateBookRequestValidator(), new BookRequestValidator(), new BookSearchRequestValidator(), new FakeTimeProvider());

        _books.GetViewAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => View(call.Arg<Guid>()));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static BookResponse View(Guid id) =>
        new(id, "Dune", "Frank Herbert", "", "Science Fiction", null, 1965, null, DateTime.UtcNow, 2, 2, false, 0, 0, 0, 0, 0);

    private static CreateBookRequest NewBook(int copies = 2, string? isbn = null) => new()
    {
        Title = "Dune",
        Author = "Frank Herbert",
        Genre = "Science Fiction",
        Isbn = isbn,
        Copies = copies
    };

    [Fact]
    public async Task New_book_gets_its_copies_and_clears_the_catalog_cache()
    {
        await _service.CreateAsync(NewBook(copies: 3), Token);

        await _books.Received(1).AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        await _copies.Received(3).AddAsync(Arg.Any<BookCopy>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, _cache.Invalidations);
    }

    [Fact]
    public async Task Ebook_only_book_has_no_copies()
    {
        await _service.CreateAsync(NewBook(copies: 0), Token);

        await _copies.DidNotReceive().AddAsync(Arg.Any<BookCopy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Isbn_is_normalized_and_must_be_unique()
    {
        _books.IsIsbnInUseAsync("9780441013593", null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(NewBook(isbn: "978-0-441-01359-3"), Token));
    }

    [Fact]
    public async Task Book_on_loan_cannot_be_deleted()
    {
        var book = TestData.Book();
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _loans.HasActiveLoanForBookAsync(book.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(book.Id, Token));
        Assert.False(book.IsDeleted);
    }

    [Fact]
    public async Task Available_book_is_soft_deleted()
    {
        var book = TestData.Book();
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        await _service.DeleteAsync(book.Id, Token);

        Assert.True(book.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_book_is_not_found()
    {
        _books.GetViewAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns((BookResponse?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), Token));
    }

    [Fact]
    public async Task Search_results_are_cached_by_the_whole_query()
    {
        var request = new BookSearchRequest { Search = "Dune", Genre = "Fiction", Sort = BookSort.Rating };
        _books.SearchAsync(request, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new PagedResult<BookResponse>([], 1, 10, 0));

        await _service.SearchAsync(request, Token);

        Assert.Equal(request.CacheKey, Assert.Single(_cache.Keys));
        Assert.Contains("rating", request.CacheKey);
    }

    [Fact]
    public async Task Suggestions_need_at_least_two_characters()
    {
        Assert.Empty(await _service.SuggestAsync(" d ", Token));
        await _books.DidNotReceive().SuggestAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Also_read_keeps_the_ranking_order()
    {
        var book = TestData.Book();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _reports.GetAlsoReadBookIdsAsync(book.Id, BookService.AlsoReadLimit, Arg.Any<CancellationToken>()).Returns([first, second]);
        _books.ListViewsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([View(second), View(first)]);

        var result = await _service.ListAlsoReadAsync(book.Id, Token);

        Assert.Equal([first, second], result.Select(view => view.Id));
    }
}
