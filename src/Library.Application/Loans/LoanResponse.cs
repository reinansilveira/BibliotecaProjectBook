namespace Library.Application.Loans;

public sealed record LoanResponse(
    Guid Id,
    Guid BookId,
    Guid MemberId,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnedAt,
    string Status);
