using Library.Domain.Entities;

namespace Library.Application.Books.Inventory;

internal static class BookCopyFactory
{
    public static IEnumerable<BookCopy> Create(Guid bookId, int quantity, DateTime now) =>
        Enumerable.Range(0, quantity).Select(_ => new BookCopy(bookId, NewBarcode(), now));

    private static string NewBarcode() => $"CP-{Guid.NewGuid():N}"[..13].ToUpperInvariant();
}
