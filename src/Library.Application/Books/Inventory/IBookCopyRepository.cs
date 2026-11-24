using Library.Domain.Entities;

namespace Library.Application.Books.Inventory;

public interface IBookCopyRepository
{
    Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopyResponse>> ListByBookAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<int> CountInCirculationAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<BookCopy?> FindFreeCopyAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<bool> IsOnLoanAsync(Guid copyId, CancellationToken cancellationToken = default);

    Task AddAsync(BookCopy copy, CancellationToken cancellationToken = default);
}
