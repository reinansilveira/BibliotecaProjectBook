namespace Library.Application.Abstractions;

public interface ICurrentUser
{
    Guid? MemberId { get; }

    string Name { get; }
}
