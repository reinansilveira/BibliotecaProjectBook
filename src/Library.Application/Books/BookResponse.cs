namespace Library.Application.Books;

public sealed record BookResponse(Guid Id, string Title, string Author, string Description, DateTime CreatedAt);
