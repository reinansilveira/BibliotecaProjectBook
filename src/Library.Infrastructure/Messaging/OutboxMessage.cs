namespace Library.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public const int MaxErrorLength = 2000;

    private OutboxMessage()
    {
    }

    public OutboxMessage(Guid id, string type, string payload, DateTime occurredAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public void MarkAsProcessed(DateTime processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void RegisterFailure(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
