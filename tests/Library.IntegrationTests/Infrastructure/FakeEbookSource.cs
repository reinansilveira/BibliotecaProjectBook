using Library.Application.Books.Inventory;

namespace Library.IntegrationTests.Infrastructure;

public sealed class FakeEbookSource : IEbookSource
{
    public Task<byte[]> DownloadGutenbergEpubAsync(int gutenbergId, CancellationToken cancellationToken = default) =>
        Task.FromResult(TestFiles.Epub($"gutenberg-{gutenbergId}"));
}
