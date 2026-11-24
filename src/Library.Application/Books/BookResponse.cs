namespace Library.Application.Books;

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string Description,
    string Genre,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    DateTime CreatedAt,
    int TotalCopies,
    int AvailableCopies,
    bool HasEbook,
    int EbookLicenses,
    int AvailableEbookLicenses,
    int WaitingCount,
    decimal AverageRating,
    int RatingsCount);
