using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Loans;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class BookServiceTests
{
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly ILoanRepository _loans = Substitute.For<ILoanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BookService _service;

    public BookServiceTests()
    {
        _service = new BookService(_books, _loans, _unitOfWork, new BookRequestValidator(), new FakeTimeProvider());
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Book_on_loan_cannot_be_deleted()
    {
        var book = TestData.Book();
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _loans.IsBookOnLoanAsync(book.Id, Arg.Any<CancellationToken>()).Returns(true);

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
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleted_book_is_not_found()
    {
        var book = TestData.Book();
        book.Delete();
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(book.Id, Token));
    }
}
