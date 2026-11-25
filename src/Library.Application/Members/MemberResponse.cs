namespace Library.Application.Members;

public sealed record MemberResponse(
    Guid Id,
    string Name,
    DateTime BirthDate,
    string Phone,
    string Email,
    string Role,
    bool EmailConfirmed,
    DateTime CreatedAt);
