namespace Library.Domain.Entities;

public class Notification
{
    private Notification()
    {
    }

    public Notification(Guid memberId, string type, string title, string message, string? link, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        MemberId = memberId;
        Type = type;
        Title = title;
        Message = message;
        Link = link;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string? Link { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    public void MarkRead(DateTime now)
    {
        ReadAt ??= now;
    }
}
