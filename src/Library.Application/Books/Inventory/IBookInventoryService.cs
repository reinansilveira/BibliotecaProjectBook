namespace Library.Application.Books.Inventory;

public interface IBookInventoryService
{
    Task<IReadOnlyList<BookCopyResponse>> ListCopiesAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopyResponse>> AddCopiesAsync(Guid bookId, AddCopiesRequest request, CancellationToken cancellationToken = default);

    Task WithdrawCopyAsync(Guid bookId, Guid copyId, CancellationToken cancellationToken = default);

    Task<BookResponse> UploadEbookAsync(Guid bookId, EbookUpload upload, CancellationToken cancellationToken = default);

    Task<BookResponse> ImportGutenbergEbookAsync(Guid bookId, GutenbergImportRequest request, CancellationToken cancellationToken = default);

    Task<BookResponse> ChangeEbookLicensesAsync(Guid bookId, ChangeLicensesRequest request, CancellationToken cancellationToken = default);
}
