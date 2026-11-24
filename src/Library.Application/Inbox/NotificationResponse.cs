namespace Library.Application.Inbox;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? Link,
    DateTime CreatedAt,
    DateTime? ReadAt);
