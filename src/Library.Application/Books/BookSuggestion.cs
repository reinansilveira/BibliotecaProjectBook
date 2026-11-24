namespace Library.Application.Books;

public sealed record BookSuggestion(Guid Id, string Title, string Author, string? CoverUrl);
