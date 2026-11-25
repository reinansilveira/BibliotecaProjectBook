using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class BookInventoryServiceTests
{
    private static readonly byte[] Epub = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3];

    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IBookCopyRepository _copies = Substitute.For<IBookCopyRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IEbookSource _source = Substitute.For<IEbookSource>();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly BookInventoryService _service;
    private readonly Book _book = TestData.Book();

    public BookInventoryServiceTests()
    {
        var loans = Substitute.For<ILoanRepository>();
        var reservations = Substitute.For<IReservationRepository>();
        var queue = new ReservationQueue(_copies, loans, reservations, Substitute.For<IMemberRepository>(), new RecordingInbox(), new RecordingEventPublisher(), _unitOfWork);

        _service = new BookInventoryService(
            _books, _copies, _storage, _source, queue, new PassThroughCatalogCache(), _unitOfWork, new AddCopiesRequestValidator(), new FakeTimeProvider());

        _books.GetByIdAsync(_book.Id, Arg.Any<CancellationToken>()).Returns(_book);
        _books.GetViewAsync(_book.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new BookResponse(_book.Id, _book.Title, _book.Author, "", "Fiction", null, null, null, DateTime.UtcNow, 0, 0, true, 2, 2, 0, 0, 0));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Uploaded_epub_is_stored_and_attached()
    {
        await _service.UploadEbookAsync(_book.Id, new EbookUpload("dune.epub", Epub, 3), Token);

        await _storage.Received(1).SaveAsync(Arg.Is<string>(key => key.EndsWith(".epub")), Arg.Is<FileContent>(file => file.ContentType == EbookFiles.EpubContentType), Arg.Any<CancellationToken>());
        Assert.True(_book.HasEbook);
        Assert.Equal(3, _book.EbookLicenses);
    }

    [Fact]
    public async Task Replacing_an_ebook_deletes_the_old_file()
    {
        _book.AttachEbook("ebooks/old.epub", 1);

        await _service.UploadEbookAsync(_book.Id, new EbookUpload("dune.epub", Epub, 1), Token);

        await _storage.Received(1).DeleteAsync("ebooks/old.epub", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task File_that_is_not_an_epub_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => _service.UploadEbookAsync(_book.Id, new EbookUpload("virus.epub", [1, 2, 3], 1), Token));
        Assert.False(_book.HasEbook);
    }

    [Fact]
    public async Task Gutenberg_import_downloads_the_epub()
    {
        _source.DownloadGutenbergEpubAsync(84, Arg.Any<CancellationToken>()).Returns(Epub);

        await _service.ImportGutenbergEbookAsync(_book.Id, new GutenbergImportRequest(84), Token);

        Assert.True(_book.HasEbook);
    }

    [Fact]
    public async Task Copy_on_loan_cannot_be_withdrawn()
    {
        var copy = new BookCopy(_book.Id, "CP-1", DateTime.UtcNow);
        _copies.GetByIdAsync(copy.Id, Arg.Any<CancellationToken>()).Returns(copy);
        _copies.IsOnLoanAsync(copy.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainException>(() => _service.WithdrawCopyAsync(_book.Id, copy.Id, Token));
        Assert.False(copy.IsWithdrawn);
    }

    [Fact]
    public async Task Copy_of_another_book_is_not_found()
    {
        var copy = new BookCopy(Guid.NewGuid(), "CP-2", DateTime.UtcNow);
        _copies.GetByIdAsync(copy.Id, Arg.Any<CancellationToken>()).Returns(copy);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.WithdrawCopyAsync(_book.Id, copy.Id, Token));
    }

    [Fact]
    public async Task Adding_copies_locks_the_book()
    {
        await _service.AddCopiesAsync(_book.Id, new AddCopiesRequest(2), Token);

        await _copies.Received(2).AddAsync(Arg.Any<BookCopy>(), Arg.Any<CancellationToken>());
        Assert.Single(_unitOfWork.Locks);
    }
}
