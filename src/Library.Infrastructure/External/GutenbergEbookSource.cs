using System.Net;
using Library.Application.Books.Inventory;
using Library.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.External;

public sealed class GutenbergEbookSource : IEbookSource
{
    public static readonly IReadOnlyList<Uri> Mirrors =
    [
        new("https://www.gutenberg.org/"),
        new("https://gutenberg.pglaf.org/")
    ];

    public static readonly TimeSpan MirrorTimeout = TimeSpan.FromMinutes(2);

    private const int BufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly ILogger<GutenbergEbookSource> _logger;

    public GutenbergEbookSource(HttpClient httpClient, ILogger<GutenbergEbookSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public static IEnumerable<string> FileNames(int gutenbergId) =>
    [
        $"cache/epub/{gutenbergId}/pg{gutenbergId}-images-3.epub",
        $"cache/epub/{gutenbergId}/pg{gutenbergId}.epub"
    ];

    public async Task<byte[]> DownloadGutenbergEpubAsync(int gutenbergId, CancellationToken cancellationToken = default)
    {
        foreach (var fileName in FileNames(gutenbergId))
        {
            foreach (var mirror in Mirrors)
            {
                var download = await TryDownloadAsync(new Uri(mirror, fileName), cancellationToken);

                if (download.Content is not null)
                {
                    return download.Content;
                }

                if (download.TooLarge)
                {
                    break;
                }
            }
        }

        throw new DomainException($"Project Gutenberg has no EPUB under {EbookFiles.MaxSizeInBytes / (1024 * 1024)} MB for book {gutenbergId}, or it is unavailable right now.");
    }

    private async Task<Download> TryDownloadAsync(Uri address, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Project Gutenberg answered {StatusCode} for {Address}.", (int)response.StatusCode, address);

                return Download.Failed;
            }

            if (response.Content.Headers.ContentLength > EbookFiles.MaxSizeInBytes)
            {
                _logger.LogInformation("{Address} is larger than the allowed size; trying a smaller edition.", address);

                return Download.Oversized;
            }

            var content = await ReadLimitedAsync(response.Content, cancellationToken);

            return content is null ? Download.Oversized : new Download(content, false);
        }
        catch (Exception exception) when (IsUnavailable(exception) && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Could not reach {Address}.", address);

            return Download.Failed;
        }
    }

    private static bool IsUnavailable(Exception exception) =>
        exception is TaskCanceledException or HttpRequestException { StatusCode: null or >= HttpStatusCode.InternalServerError };

    private static async Task<byte[]?> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[BufferSize];
        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > EbookFiles.MaxSizeInBytes)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }

    private sealed record Download(byte[]? Content, bool TooLarge)
    {
        public static readonly Download Failed = new(null, false);

        public static readonly Download Oversized = new(null, true);
    }
}
