namespace Library.Application.Reviews;

public sealed record ReviewResponse(
    Guid Id,
    Guid BookId,
    Guid MemberId,
    string ReviewerName,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt);
