using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class BlacklistEntryConfiguration : IEntityTypeConfiguration<BlacklistEntry>
{
    public void Configure(EntityTypeBuilder<BlacklistEntry> builder)
    {
        builder.ToTable("BlacklistEntries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.MemberName).HasMaxLength(150).IsRequired();

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(entry => entry.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.MemberId).IsUnique();
    }
}
