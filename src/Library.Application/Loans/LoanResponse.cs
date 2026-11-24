namespace Library.Application.Loans;

public sealed record LoanResponse(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string BookAuthor,
    string? CoverUrl,
    Guid MemberId,
    string Format,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnedAt,
    string Status,
    int RenewalsLeft,
    decimal ReadingProgress,
    string? ReadingLocation);
