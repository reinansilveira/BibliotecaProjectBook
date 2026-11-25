using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Application.Loans.Reading;

public sealed class EbookReadingService : IEbookReadingService
{
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(10);

    private const string InvalidLink = "The reading link is invalid or has expired.";

    private readonly ILoanRepository _loans;
    private readonly IBookRepository _books;
    private readonly IFileStorage _storage;
    private readonly IEbookLinkSigner _signer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReadingProgressRequest> _progressValidator;
    private readonly ILoanService _loanService;
    private readonly TimeProvider _timeProvider;

    public EbookReadingService(
        ILoanRepository loans,
        IBookRepository books,
        IFileStorage storage,
        IEbookLinkSigner signer,
        IUnitOfWork unitOfWork,
        IValidator<ReadingProgressRequest> progressValidator,
        ILoanService loanService,
        TimeProvider timeProvider)
    {
        _loans = loans;
        _books = books;
        _storage = storage;
        _signer = signer;
        _unitOfWork = unitOfWork;
        _progressValidator = progressValidator;
        _loanService = loanService;
        _timeProvider = timeProvider;
    }

    private DateTimeOffset Now => _timeProvider.GetUtcNow();

    public async Task<EbookAccess> CreateAccessAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        var loan = await GetActiveEbookLoanAsync(loanId, cancellationToken);
        var book = await _books.GetByIdAsync(loan.BookId, cancellationToken);

        if (book?.EbookFileKey is null)
        {
            throw new DomainException("This e-book is not available anymore.");
        }

        var expiresAt = Now.Add(LinkLifetime);
        var expires = expiresAt.ToUnixTimeSeconds();
        var format = book.EbookFileKey.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "epub";

        return new EbookAccess(loan.Id, expires, _signer.Sign(loan.Id, expires), expiresAt.UtcDateTime, format);
    }

    public async Task<FileContent> OpenAsync(Guid loanId, long expires, string signature, CancellationToken cancellationToken = default)
    {
        if (expires < Now.ToUnixTimeSeconds() || !_signer.Verify(loanId, expires, signature))
        {
            throw new UnauthorizedException(InvalidLink);
        }

        var loan = await _loans.GetByIdAsync(loanId, cancellationToken);

        if (loan is null || loan.Format != BookFormat.Ebook || !loan.IsActive(Now.UtcDateTime))
        {
            throw new UnauthorizedException(InvalidLink);
        }

        var book = await _books.GetByIdAsync(loan.BookId, cancellationToken);

        if (book?.EbookFileKey is null)
        {
            throw new NotFoundException("E-book not found.");
        }

        return await _storage.GetAsync(book.EbookFileKey, cancellationToken) ?? throw new NotFoundException("E-book not found.");
    }

    public async Task<LoanResponse> UpdateProgressAsync(Guid loanId, ReadingProgressRequest request, CancellationToken cancellationToken = default)
    {
        await _progressValidator.ValidateAndThrowAsync(request, cancellationToken);

        var loan = await _loans.GetByIdAsync(loanId, cancellationToken) ?? throw new NotFoundException("Loan not found.");

        loan.UpdateReadingProgress(request.Location, request.Percentage, Now.UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _loanService.GetByIdAsync(loanId, cancellationToken);
    }

    private async Task<Loan> GetActiveEbookLoanAsync(Guid loanId, CancellationToken cancellationToken)
    {
        var loan = await _loans.GetByIdAsync(loanId, cancellationToken) ?? throw new NotFoundException("Loan not found.");

        if (loan.Format != BookFormat.Ebook)
        {
            throw new DomainException("Only e-book loans can be read online.");
        }

        if (!loan.IsActive(Now.UtcDateTime))
        {
            throw new DomainException("This e-book loan is no longer active.");
        }

        return loan;
    }
}
