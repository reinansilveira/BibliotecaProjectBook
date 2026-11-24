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

    public async Task<byte[]> DownloadGutenbergEpubAsync(int gutenbergId, CancellationToken cancellationToken = default)
    {
        foreach (var mirror in Mirrors)
        {
            var content = await TryDownloadAsync(new Uri(mirror, $"cache/epub/{gutenbergId}/pg{gutenbergId}-images-3.epub"), cancellationToken);

            if (content is not null)
            {
                return content;
            }
        }

        throw new DomainException($"Project Gutenberg has no EPUB for book {gutenbergId}, or it is unavailable right now.");
    }

    private async Task<byte[]?> TryDownloadAsync(Uri address, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Project Gutenberg answered {StatusCode} for {Address}.", (int)response.StatusCode, address);

                return null;
            }

            if (response.Content.Headers.ContentLength > EbookFiles.MaxSizeInBytes)
            {
                throw new DomainException("The e-book is larger than the allowed size.");
            }

            return await ReadLimitedAsync(response.Content, cancellationToken);
        }
        catch (Exception exception) when (IsUnavailable(exception) && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Could not reach {Address}.", address);

            return null;
        }
    }

    private static bool IsUnavailable(Exception exception) =>
        exception is TaskCanceledException or HttpRequestException { StatusCode: null or >= HttpStatusCode.InternalServerError };

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[BufferSize];
        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > EbookFiles.MaxSizeInBytes)
            {
                throw new DomainException("The e-book is larger than the allowed size.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }
}
