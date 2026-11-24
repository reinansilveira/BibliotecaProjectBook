namespace Library.Application.Loans;

public sealed record CreateLoanRequest(Guid BookId, Guid MemberId);
