namespace Library.Application.Books.Inventory;

public interface IEbookSource
{
    Task<byte[]> DownloadGutenbergEpubAsync(int gutenbergId, CancellationToken cancellationToken = default);
}
