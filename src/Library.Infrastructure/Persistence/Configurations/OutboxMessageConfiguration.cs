using Library.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);

        builder.HasIndex(message => message.OccurredAt).HasFilter("[ProcessedAt] IS NULL");
    }
}
