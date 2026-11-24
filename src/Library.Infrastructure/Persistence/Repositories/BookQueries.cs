using Library.Application.Books;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Repositories;

internal static class BookQueries
{
    public static IQueryable<BookResponse> ToViews(this IQueryable<Book> books, LibraryDbContext context, DateTime now) =>
        books.Select(book => new BookResponse(
            book.Id,
            book.Title,
            book.Author,
            book.Description,
            book.Genre,
            book.Isbn,
            book.PublishedYear,
            book.CoverUrl,
            book.CreatedAt,
            context.BookCopies.Count(copy => copy.BookId == book.Id && copy.WithdrawnAt == null),
            context.BookCopies.Count(copy => copy.BookId == book.Id && copy.WithdrawnAt == null)
                - context.Loans.Count(loan => loan.BookId == book.Id && loan.Format == BookFormat.Print && loan.ReturnedAt == null)
                - context.Reservations.Count(reservation => reservation.BookId == book.Id && reservation.Format == BookFormat.Print && reservation.Status == ReservationStatus.Ready),
            book.EbookFileKey != null && book.EbookLicenses > 0,
            book.EbookFileKey == null ? 0 : book.EbookLicenses,
            book.EbookFileKey == null
                ? 0
                : book.EbookLicenses
                    - context.Loans.Count(loan => loan.BookId == book.Id && loan.Format == BookFormat.Ebook && loan.ReturnedAt == null && loan.DueDate > now)
                    - context.Reservations.Count(reservation => reservation.BookId == book.Id && reservation.Format == BookFormat.Ebook && reservation.Status == ReservationStatus.Ready),
            context.Reservations.Count(reservation => reservation.BookId == book.Id && reservation.Status == ReservationStatus.Waiting),
            book.AverageRating,
            book.RatingsCount));

    public static IQueryable<Book> WhereAvailable(this IQueryable<Book> books, LibraryDbContext context, DateTime now) =>
        books.Where(book =>
            context.BookCopies.Count(copy => copy.BookId == book.Id && copy.WithdrawnAt == null)
                - context.Loans.Count(loan => loan.BookId == book.Id && loan.Format == BookFormat.Print && loan.ReturnedAt == null)
                - context.Reservations.Count(reservation => reservation.BookId == book.Id && reservation.Format == BookFormat.Print && reservation.Status == ReservationStatus.Ready) > 0
            || (book.EbookFileKey != null
                && book.EbookLicenses
                    - context.Loans.Count(loan => loan.BookId == book.Id && loan.Format == BookFormat.Ebook && loan.ReturnedAt == null && loan.DueDate > now)
                    - context.Reservations.Count(reservation => reservation.BookId == book.Id && reservation.Format == BookFormat.Ebook && reservation.Status == ReservationStatus.Ready) > 0));

    public static BookResponse Normalize(this BookResponse view) => view with
    {
        AvailableCopies = Math.Max(0, view.AvailableCopies),
        AvailableEbookLicenses = Math.Max(0, view.AvailableEbookLicenses)
    };
}
