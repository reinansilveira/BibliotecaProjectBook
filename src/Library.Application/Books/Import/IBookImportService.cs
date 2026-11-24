namespace Library.Application.Books.Import;

public interface IBookImportService
{
    Task<BookMetadata> LookupAsync(string isbn, CancellationToken cancellationToken = default);

    Task<BookResponse> ImportAsync(ImportBookRequest request, CancellationToken cancellationToken = default);
}
