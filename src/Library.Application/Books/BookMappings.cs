using Library.Domain.Entities;

namespace Library.Application.Books;

internal static class BookMappings
{
    public static BookResponse ToResponse(this Book book) =>
        new(book.Id, book.Title, book.Author, book.Description, book.CreatedAt);
}
