using FluentValidation;
using Library.Application.Books;
using Library.Application.Books.Import;
using Library.Application.Common.Exceptions;
using NSubstitute;

namespace Library.UnitTests.Application;

public class BookImportServiceTests
{
    private readonly IBookMetadataProvider _metadata = Substitute.For<IBookMetadataProvider>();
    private readonly IBookService _books = Substitute.For<IBookService>();
    private readonly BookImportService _service;

    public BookImportServiceTests()
    {
        _service = new BookImportService(_metadata, _books);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Invalid_isbn_fails_validation_without_calling_open_library()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.LookupAsync("123", Token));
        await _metadata.DidNotReceive().FindByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_isbn_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.LookupAsync("978-0-441-01359-3", Token));
    }

    [Fact]
    public async Task Import_creates_the_book_with_the_metadata_and_the_chosen_genre()
    {
        _metadata.FindByIsbnAsync("9780441013593", Arg.Any<CancellationToken>())
            .Returns(new BookMetadata("9780441013593", "Dune", "Frank Herbert", new string('x', 3000), "Fiction", 1965, "https://covers.example/dune.jpg"));

        await _service.ImportAsync(new ImportBookRequest("9780441013593", 2, "Science Fiction"), Token);

        await _books.Received(1).CreateAsync(
            Arg.Is<CreateBookRequest>(request =>
                request.Title == "Dune"
                && request.Genre == "Science Fiction"
                && request.Description.Length == 2000
                && request.Copies == 2
                && request.PublishedYear == 1965),
            Arg.Any<CancellationToken>());
    }
}
