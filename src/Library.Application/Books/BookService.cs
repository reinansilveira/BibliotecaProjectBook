using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Books;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly ILoanRepository _loans;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<BookRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public BookService(
        IBookRepository books,
        ILoanRepository loans,
        IUnitOfWork unitOfWork,
        IValidator<BookRequest> validator,
        TimeProvider timeProvider)
    {
        _books = books;
        _loans = loans;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<BookResponse> CreateAsync(BookRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var book = new Book(request.Title, request.Author, request.Description, _timeProvider.GetUtcNow().UtcDateTime);

        await _books.AddAsync(book, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return book.ToResponse();
    }

    public async Task<BookResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await GetActiveBookAsync(id, cancellationToken);

        return book.ToResponse();
    }

    public async Task<PagedResult<BookResponse>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Search.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _books.ListAsync(request, cancellationToken);

        return page.Map(book => book.ToResponse());
    }

    public async Task<BookResponse> UpdateAsync(Guid id, BookRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var book = await GetActiveBookAsync(id, cancellationToken);

        book.Update(request.Title, request.Author, request.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return book.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await GetActiveBookAsync(id, cancellationToken);

        if (await _loans.IsBookOnLoanAsync(id, cancellationToken))
        {
            throw new DomainException("A book that is on loan cannot be deleted.");
        }

        book.Delete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Book> GetActiveBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(id, cancellationToken);

        if (book is null || book.IsDeleted)
        {
            throw new NotFoundException("Book not found.");
        }

        return book;
    }
}
