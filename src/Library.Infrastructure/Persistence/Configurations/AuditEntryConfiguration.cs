using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.ActorName).HasMaxLength(150).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.EntityId).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.Changes).IsRequired();

        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId });
        builder.HasIndex(entry => entry.ActorId);
    }
}
