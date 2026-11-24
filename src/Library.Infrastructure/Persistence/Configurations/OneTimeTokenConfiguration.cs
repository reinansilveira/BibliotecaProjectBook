using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class OneTimeTokenConfiguration : IEntityTypeConfiguration<OneTimeToken>
{
    public void Configure(EntityTypeBuilder<OneTimeToken> builder)
    {
        builder.ToTable("OneTimeTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Purpose).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(token => token.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.MemberId, token.Purpose });
    }
}
