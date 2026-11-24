using Library.Domain.Entities;

namespace Library.Application.Books;

public record BookRequest
{
    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Genre { get; init; } = string.Empty;

    public string? Isbn { get; init; }

    public int? PublishedYear { get; init; }

    public string? CoverUrl { get; init; }

    internal BookDetails ToDetails() => new(
        Title.Trim(),
        Author.Trim(),
        Description.Trim(),
        Genre.Trim(),
        Books.Isbn.Normalize(Isbn),
        PublishedYear,
        string.IsNullOrWhiteSpace(CoverUrl) ? null : CoverUrl.Trim());
}
