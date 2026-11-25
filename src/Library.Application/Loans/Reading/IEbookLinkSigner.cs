namespace Library.Application.Loans.Reading;

public interface IEbookLinkSigner
{
    string Sign(Guid loanId, long expiresAtUnixSeconds);

    bool Verify(Guid loanId, long expiresAtUnixSeconds, string signature);
}
