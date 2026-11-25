namespace Library.Application.Books.Import;

public sealed record BookMetadata(
    string Isbn,
    string Title,
    string Author,
    string Description,
    string Genre,
    int? PublishedYear,
    string? CoverUrl);
