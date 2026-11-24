using System.Net;
using Library.Application.Abstractions;
using Library.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Library.Infrastructure.Notifications;

public sealed class SendGridEmailSender : IEmailSender
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IOptions<SendGridSettings> settings, ILogger<SendGridEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            _logger.LogWarning("SendGrid is not configured. Email \"{Subject}\" to {To} was not sent.", message.Subject, message.To);
            return;
        }

        var email = MailHelper.CreateSingleEmail(
            new EmailAddress(_settings.FromEmail, _settings.FromName),
            new EmailAddress(message.To),
            message.Subject,
            message.Body,
            $"<p>{WebUtility.HtmlEncode(message.Body)}</p>");

        var client = new SendGridClient(_settings.ApiKey);
        var response = await client.SendEmailAsync(email, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException($"SendGrid returned {(int)response.StatusCode} while sending \"{message.Subject}\".");
    }
}
