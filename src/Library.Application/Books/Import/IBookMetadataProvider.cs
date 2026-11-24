namespace Library.Application.Books.Import;

public interface IBookMetadataProvider
{
    Task<BookMetadata?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}
