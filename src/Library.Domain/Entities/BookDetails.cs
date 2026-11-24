namespace Library.Domain.Entities;

public sealed record BookDetails(
    string Title,
    string Author,
    string Description,
    string Genre,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl);
