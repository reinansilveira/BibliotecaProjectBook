using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Reservations;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Application.Books.Inventory;

public sealed class BookInventoryService : IBookInventoryService
{
    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly IFileStorage _storage;
    private readonly IEbookSource _ebookSource;
    private readonly ReservationQueue _queue;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AddCopiesRequest> _addCopiesValidator;
    private readonly TimeProvider _timeProvider;

    public BookInventoryService(
        IBookRepository books,
        IBookCopyRepository copies,
        IFileStorage storage,
        IEbookSource ebookSource,
        ReservationQueue queue,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IValidator<AddCopiesRequest> addCopiesValidator,
        TimeProvider timeProvider)
    {
        _books = books;
        _copies = copies;
        _storage = storage;
        _ebookSource = ebookSource;
        _queue = queue;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _addCopiesValidator = addCopiesValidator;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<BookCopyResponse>> ListCopiesAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        await GetActiveBookAsync(bookId, cancellationToken);

        return await _copies.ListByBookAsync(bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<BookCopyResponse>> AddCopiesAsync(Guid bookId, AddCopiesRequest request, CancellationToken cancellationToken = default)
    {
        await _addCopiesValidator.ValidateAndThrowAsync(request, cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(bookId), token);
            var book = await GetActiveBookAsync(bookId, token);

            foreach (var copy in BookCopyFactory.Create(bookId, request.Quantity, Now))
            {
                await _copies.AddAsync(copy, token);
            }

            await _unitOfWork.SaveChangesAsync(token);
            await _queue.PromoteAsync(book, BookFormat.Print, Now, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return await _copies.ListByBookAsync(bookId, cancellationToken);
    }

    public async Task WithdrawCopyAsync(Guid bookId, Guid copyId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(bookId), token);
            await GetActiveBookAsync(bookId, token);

            var copy = await _copies.GetByIdAsync(copyId, token);

            if (copy is null || copy.BookId != bookId || copy.IsWithdrawn)
            {
                throw new NotFoundException("Copy not found.");
            }

            if (await _copies.IsOnLoanAsync(copyId, token))
            {
                throw new DomainException("A copy that is on loan cannot be withdrawn.");
            }

            copy.Withdraw(Now);
            await _unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);
    }

    public async Task<BookResponse> UploadEbookAsync(Guid bookId, EbookUpload upload, CancellationToken cancellationToken = default)
    {
        var contentType = EbookFiles.DetectContentType(upload.FileName, upload.Content);

        return await AttachEbookAsync(bookId, new FileContent(Path.GetFileName(upload.FileName), contentType, upload.Content), upload.Licenses, cancellationToken);
    }

    public async Task<BookResponse> ImportGutenbergEbookAsync(Guid bookId, GutenbergImportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.GutenbergId <= 0)
        {
            throw new DomainException("The Project Gutenberg id must be a positive number.");
        }

        await GetActiveBookAsync(bookId, cancellationToken);

        var content = await _ebookSource.DownloadGutenbergEpubAsync(request.GutenbergId, cancellationToken);
        var fileName = $"pg{request.GutenbergId}.epub";
        var contentType = EbookFiles.DetectContentType(fileName, content);

        return await AttachEbookAsync(bookId, new FileContent(fileName, contentType, content), request.Licenses, cancellationToken);
    }

    public async Task<BookResponse> ChangeEbookLicensesAsync(Guid bookId, ChangeLicensesRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(bookId), token);
            var book = await GetActiveBookAsync(bookId, token);

            book.ChangeEbookLicenses(request.Licenses);
            await _unitOfWork.SaveChangesAsync(token);
            await _queue.PromoteAsync(book, BookFormat.Ebook, Now, token);
        }, cancellationToken);

        await _cache.InvalidateAsync(cancellationToken);

        return await GetViewAsync(bookId, cancellationToken);
    }

    private async Task<BookResponse> AttachEbookAsync(Guid bookId, FileContent file, int licenses, CancellationToken cancellationToken)
    {
        var key = $"ebooks/{bookId:N}/{Guid.NewGuid():N}{EbookFiles.ExtensionFor(file.ContentType)}";
        string? previousKey = null;

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _unitOfWork.AcquireLockAsync(ReservationQueue.LockFor(bookId), token);
            var book = await GetActiveBookAsync(bookId, token);
            previousKey = book.EbookFileKey;

            await _storage.SaveAsync(key, file, token);
            book.AttachEbook(key, licenses);
            await _unitOfWork.SaveChangesAsync(token);
            await _queue.PromoteAsync(book, BookFormat.Ebook, Now, token);
        }, cancellationToken);

        if (previousKey is not null)
        {
            await _storage.DeleteAsync(previousKey, cancellationToken);
        }

        await _cache.InvalidateAsync(cancellationToken);

        return await GetViewAsync(bookId, cancellationToken);
    }

    private async Task<BookResponse> GetViewAsync(Guid bookId, CancellationToken cancellationToken) =>
        await _books.GetViewAsync(bookId, Now, cancellationToken) ?? throw new NotFoundException("Book not found.");

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
