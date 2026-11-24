using Library.Application.Notifications;

namespace Library.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
