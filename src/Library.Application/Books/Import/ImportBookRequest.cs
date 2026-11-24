namespace Library.Application.Books.Import;

public sealed record ImportBookRequest(string Isbn, int Copies = 1, string? Genre = null);
