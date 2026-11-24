using Library.Application.Abstractions;

namespace Library.Application.Loans.Reading;

public interface IEbookReadingService
{
    Task<EbookAccess> CreateAccessAsync(Guid loanId, CancellationToken cancellationToken = default);

    Task<FileContent> OpenAsync(Guid loanId, long expires, string signature, CancellationToken cancellationToken = default);

    Task<LoanResponse> UpdateProgressAsync(Guid loanId, ReadingProgressRequest request, CancellationToken cancellationToken = default);
}
