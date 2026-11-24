using Library.Application.Books.Inventory;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class BookCopyRepository : IBookCopyRepository
{
    private readonly LibraryDbContext _context;

    public BookCopyRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.BookCopies.FirstOrDefaultAsync(copy => copy.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BookCopyResponse>> ListByBookAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        await _context.BookCopies
            .AsNoTracking()
            .Where(copy => copy.BookId == bookId)
            .OrderBy(copy => copy.WithdrawnAt != null)
            .ThenBy(copy => copy.AddedAt)
            .ThenBy(copy => copy.Barcode)
            .Select(copy => new BookCopyResponse(
                copy.Id,
                copy.Barcode,
                copy.AddedAt,
                copy.WithdrawnAt,
                _context.Loans.Any(loan => loan.CopyId == copy.Id && loan.ReturnedAt == null)))
            .ToListAsync(cancellationToken);

    public Task<int> CountInCirculationAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        _context.BookCopies.CountAsync(copy => copy.BookId == bookId && copy.WithdrawnAt == null, cancellationToken);

    public Task<BookCopy?> FindFreeCopyAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        _context.BookCopies
            .Where(copy => copy.BookId == bookId
                && copy.WithdrawnAt == null
                && !_context.Loans.Any(loan => loan.CopyId == copy.Id && loan.ReturnedAt == null))
            .OrderBy(copy => copy.AddedAt)
            .ThenBy(copy => copy.Barcode)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsOnLoanAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        _context.Loans.AnyAsync(loan => loan.CopyId == copyId && loan.ReturnedAt == null, cancellationToken);

    public async Task AddAsync(BookCopy copy, CancellationToken cancellationToken = default) =>
        await _context.BookCopies.AddAsync(copy, cancellationToken);
}
