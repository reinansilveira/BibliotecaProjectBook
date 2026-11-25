using System.Globalization;
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
        EmailConfirmationRequestedEvent confirmation => new EmailMessage(
            confirmation.Email,
            "Confirm your email",
            $"Hi {confirmation.Name}, confirm your email address by opening this link: {confirmation.Link}"),
        PasswordResetRequestedEvent reset => new EmailMessage(
            reset.Email,
            "Reset your password",
            $"Hi {reset.Name}, open this link to choose a new password: {reset.Link}. If you did not ask for it, ignore this email."),
        LoanCreatedEvent created => new EmailMessage(
            created.MemberEmail,
            "Loan confirmed",
            created.IsEbook
                ? $"Hi {created.MemberName}, the e-book \"{created.BookTitle}\" is ready to read until {Date(created.DueDate)}."
                : $"Hi {created.MemberName}, you borrowed \"{created.BookTitle}\". Please return it by {Date(created.DueDate)}."),
        LoanReturnedEvent returned => new EmailMessage(
            returned.MemberEmail,
            "Book returned",
            $"Hi {returned.MemberName}, we received \"{returned.BookTitle}\" on {Date(returned.ReturnedAt)}. Thank you!"),
        LoanDueSoonEvent dueSoon => new EmailMessage(
            dueSoon.MemberEmail,
            dueSoon.IsEbook ? "Your e-book loan ends tomorrow" : "Your loan is due tomorrow",
            $"Hi {dueSoon.MemberName}, \"{dueSoon.BookTitle}\" is due on {Date(dueSoon.DueDate)}."),
        LoanOverdueEvent overdue => new EmailMessage(
            overdue.MemberEmail,
            "Overdue loan",
            $"Hi {overdue.MemberName}, \"{overdue.BookTitle}\" was due on {Date(overdue.DueDate)}. A fine of {Money(overdue.DailyRate, overdue.Currency)} per day applies, and new loans are blocked until the book is returned."),
        ReservationReadyEvent ready => new EmailMessage(
            ready.MemberEmail,
            "Your reserved book is available",
            $"Hi {ready.MemberName}, \"{ready.BookTitle}\" is waiting for you until {ready.ExpiresAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC."),
        FineIssuedEvent fine => new EmailMessage(
            fine.MemberEmail,
            "Late return fine",
            $"Hi {fine.MemberName}, \"{fine.BookTitle}\" was returned {fine.DaysOverdue} day(s) late. A fine of {Money(fine.Amount, fine.Currency)} was issued."),
        _ => null
    };

    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Money(decimal amount, string currency) => $"{currency} {amount.ToString("0.00", CultureInfo.InvariantCulture)}";
}
