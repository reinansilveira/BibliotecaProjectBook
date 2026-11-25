namespace Library.Infrastructure.Seeding;

public sealed record DemoBook(
    string Title,
    string Author,
    string Genre,
    int PublishedYear,
    string Isbn,
    int GutenbergId,
    int Copies,
    string Description);
