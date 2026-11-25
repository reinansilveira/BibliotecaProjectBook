namespace Library.Application.Inbox;

public interface IInbox
{
    void Add(Guid memberId, string type, string title, string message, string? link = null);
}
