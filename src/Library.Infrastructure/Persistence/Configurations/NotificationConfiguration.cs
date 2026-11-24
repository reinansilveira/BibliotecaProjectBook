using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Ignore(notification => notification.IsRead);

        builder.Property(notification => notification.Type).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(150).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
        builder.Property(notification => notification.Link).HasMaxLength(200);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(notification => notification.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => new { notification.MemberId, notification.CreatedAt });
        builder.HasIndex(notification => notification.MemberId).HasFilter("[ReadAt] IS NULL").HasDatabaseName("IX_Notifications_Unread");
    }
}
