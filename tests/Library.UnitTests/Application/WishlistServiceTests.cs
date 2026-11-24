using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Wishlist;
using Library.Domain.Entities;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class WishlistServiceTests
{
    private readonly IWishlistRepository _wishlist = Substitute.For<IWishlistRepository>();
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly WishlistService _service;
    private readonly Book _book = TestData.Book();
    private readonly Guid _memberId = Guid.NewGuid();

    public WishlistServiceTests()
    {
        _service = new WishlistService(_wishlist, _books, _unitOfWork, new FakeTimeProvider());
        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Adding_twice_keeps_a_single_item()
    {
        _wishlist.GetAsync(_memberId, _book.Id, Arg.Any<CancellationToken>()).Returns(null, new WishlistItem(_memberId, _book.Id, DateTime.UtcNow));

        await _service.AddAsync(_memberId, _book.Id, Token);
        await _service.AddAsync(_memberId, _book.Id, Token);

        await _wishlist.Received(1).AddAsync(Arg.Any<WishlistItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_book_cannot_be_wished()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.AddAsync(_memberId, Guid.NewGuid(), Token));
    }

    [Fact]
    public async Task Removing_a_missing_item_does_nothing()
    {
        await _service.RemoveAsync(_memberId, _book.Id, Token);

        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
