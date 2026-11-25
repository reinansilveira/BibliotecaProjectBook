using Library.Application.Inbox;

namespace Library.UnitTests.Fakes;

public sealed class RecordingInbox : IInbox
{
    public List<(Guid MemberId, string Type, string Title)> Notifications { get; } = [];

    public void Add(Guid memberId, string type, string title, string message, string? link = null) =>
        Notifications.Add((memberId, type, title));
}
