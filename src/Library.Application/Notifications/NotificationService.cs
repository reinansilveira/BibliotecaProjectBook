using Library.Application.Abstractions;
using Library.Application.Events;
using Microsoft.Extensions.Logging;

namespace Library.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEmailSender emailSender, ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var message = CreateMessage(integrationEvent);

        if (message is null)
        {
            _logger.LogWarning("No notification is configured for {EventType}.", integrationEvent.GetType().Name);
            return;
        }

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public static EmailMessage? CreateMessage(IIntegrationEvent integrationEvent) => integrationEvent switch
    {
        MemberRegisteredEvent registered => new EmailMessage(
            registered.Email,
            "Welcome to the library",
            $"Hi {registered.Name}, your library account was created successfully."),
        LoanCreatedEvent created => new EmailMessage(
            created.MemberEmail,
            "Loan confirmed",
            $"Hi {created.MemberName}, you borrowed \"{created.BookTitle}\". Please return it by {created.DueDate:yyyy-MM-dd}."),
        LoanReturnedEvent returned => new EmailMessage(
            returned.MemberEmail,
            "Book returned",
            $"Hi {returned.MemberName}, we received \"{returned.BookTitle}\" on {returned.ReturnedAt:yyyy-MM-dd}. Thank you!"),
        MemberBlacklistedEvent blacklisted => new EmailMessage(
            blacklisted.MemberEmail,
            "Overdue loan",
            $"Hi {blacklisted.MemberName}, you have an overdue loan. New loans are blocked until the book is returned."),
        _ => null
    };
}
