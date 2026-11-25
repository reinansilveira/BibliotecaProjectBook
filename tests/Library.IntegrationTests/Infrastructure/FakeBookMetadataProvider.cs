using System.Collections.Concurrent;
using Library.Application.Books.Import;

namespace Library.IntegrationTests.Infrastructure;

public sealed class FakeBookMetadataProvider : IBookMetadataProvider
{
    private readonly ConcurrentDictionary<string, BookMetadata> _books = new();

    public void Add(BookMetadata metadata) => _books[metadata.Isbn] = metadata;

    public Task<BookMetadata?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default) =>
        Task.FromResult(_books.GetValueOrDefault(isbn));
}
