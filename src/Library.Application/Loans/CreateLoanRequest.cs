using Library.Domain.Enums;

namespace Library.Application.Loans;

public sealed record CreateLoanRequest(Guid BookId, Guid MemberId, BookFormat Format = BookFormat.Print);
