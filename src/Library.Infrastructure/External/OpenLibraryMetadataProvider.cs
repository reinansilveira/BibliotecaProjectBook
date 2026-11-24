using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Library.Application.Books.Import;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.External;

public sealed partial class OpenLibraryMetadataProvider : IBookMetadataProvider
{
    public const string DefaultGenre = "General";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenLibraryMetadataProvider> _logger;

    public OpenLibraryMetadataProvider(HttpClient httpClient, ILogger<OpenLibraryMetadataProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookMetadata?> FindByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        using var data = await GetJsonAsync($"api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data", cancellationToken);

        if (data is null || !data.RootElement.TryGetProperty($"ISBN:{isbn}", out var book))
        {
            return null;
        }

        var title = GetString(book, "title");

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var description = await FindDescriptionAsync(isbn, cancellationToken);

        return new BookMetadata(
            isbn,
            title,
            FirstName(book, "authors") ?? "Unknown author",
            description,
            ToGenre(FirstName(book, "subjects")),
            ParseYear(GetString(book, "publish_date")),
            book.TryGetProperty("cover", out var cover) ? GetString(cover, "large") : $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg");
    }

    public static int? ParseYear(string? publishDate)
    {
        if (string.IsNullOrWhiteSpace(publishDate))
        {
            return null;
        }

        var match = YearPattern().Match(publishDate);

        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    public static string ToGenre(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return DefaultGenre;
        }

        var genre = subject.Split(',', '-', '(')[0].Trim();

        if (genre.Length == 0)
        {
            return DefaultGenre;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genre.ToLowerInvariant());
    }

    private async Task<string> FindDescriptionAsync(string isbn, CancellationToken cancellationToken)
    {
        using var edition = await GetJsonAsync($"isbn/{isbn}.json", cancellationToken);

        if (edition is null
            || !edition.RootElement.TryGetProperty("works", out var works)
            || works.ValueKind != JsonValueKind.Array
            || works.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var workKey = GetString(works[0], "key");

        if (workKey is null)
        {
            return string.Empty;
        }

        using var work = await GetJsonAsync($"{workKey.TrimStart('/')}.json", cancellationToken);

        if (work is null || !work.RootElement.TryGetProperty("description", out var description))
        {
            return string.Empty;
        }

        var text = description.ValueKind == JsonValueKind.String ? description.GetString() : GetString(description, "value");

        return text?.Trim() ?? string.Empty;
    }

    private async Task<JsonDocument?> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound || !response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Open Library request to {Path} failed.", path);

            return null;
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? FirstName(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            return null;
        }

        return GetString(items[0], "name");
    }

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex YearPattern();
}
