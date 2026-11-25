namespace Library.Application.Members;

public sealed record UpdateMemberRequest(string Name, DateTime BirthDate, string Phone, string Email);
