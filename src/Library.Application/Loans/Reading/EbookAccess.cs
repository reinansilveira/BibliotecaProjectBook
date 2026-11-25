namespace Library.Application.Loans.Reading;

public sealed record EbookAccess(Guid LoanId, long Expires, string Signature, DateTime ExpiresAt, string FileFormat);
