namespace Library.Infrastructure.Messaging;

public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid messageId, DateTime processedAt)
    {
        MessageId = messageId;
        ProcessedAt = processedAt;
    }

    public Guid MessageId { get; private set; }

    public DateTime ProcessedAt { get; private set; }
}
