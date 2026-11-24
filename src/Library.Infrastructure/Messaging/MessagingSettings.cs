namespace Library.Infrastructure.Messaging;

public sealed class MessagingSettings
{
    public const string SectionName = "Messaging";

    public bool Enabled { get; set; } = true;

    public int OutboxPollingIntervalSeconds { get; set; } = 5;

    public int OutboxBatchSize { get; set; } = 20;

    public int MaxPublishAttempts { get; set; } = 5;
}
