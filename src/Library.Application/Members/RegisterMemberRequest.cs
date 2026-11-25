namespace Library.Application.Members;

public sealed record RegisterMemberRequest(string Name, DateTime BirthDate, string Phone, string Email, string Password);
