namespace Library.Application.Notifications;

public sealed record EmailMessage(string To, string Subject, string Body);
