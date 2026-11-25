using FluentValidation;
using FluentValidation.Results;
using Library.Application.Common.Exceptions;

namespace Library.Application.Books.Import;

public sealed class BookImportService : IBookImportService
{
    private readonly IBookMetadataProvider _metadata;
    private readonly IBookService _books;

    public BookImportService(IBookMetadataProvider metadata, IBookService books)
    {
        _metadata = metadata;
        _books = books;
    }

    public async Task<BookMetadata> LookupAsync(string isbn, CancellationToken cancellationToken = default)
    {
        if (!Isbn.IsValid(isbn))
        {
            throw new ValidationException([new ValidationFailure("Isbn", "The ISBN is not valid. Use 10 or 13 digits with a correct check digit.")]);
        }

        return await _metadata.FindByIsbnAsync(Isbn.Normalize(isbn)!, cancellationToken)
            ?? throw new NotFoundException("No book was found for this ISBN on Open Library.");
    }

    public async Task<BookResponse> ImportAsync(ImportBookRequest request, CancellationToken cancellationToken = default)
    {
        var metadata = await LookupAsync(request.Isbn, cancellationToken);

        return await _books.CreateAsync(new CreateBookRequest
        {
            Title = Truncate(metadata.Title, 200),
            Author = Truncate(metadata.Author, 150),
            Description = Truncate(metadata.Description, 2000),
            Genre = Truncate(string.IsNullOrWhiteSpace(request.Genre) ? metadata.Genre : request.Genre, 50),
            Isbn = metadata.Isbn,
            PublishedYear = metadata.PublishedYear,
            CoverUrl = metadata.CoverUrl,
            Copies = request.Copies
        }, cancellationToken);
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length].TrimEnd();
}
