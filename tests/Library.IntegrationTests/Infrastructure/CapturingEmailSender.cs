using System.Collections.Concurrent;
using Library.Application.Abstractions;
using Library.Application.Notifications;

namespace Library.IntegrationTests.Infrastructure;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);

        return Task.CompletedTask;
    }
}
