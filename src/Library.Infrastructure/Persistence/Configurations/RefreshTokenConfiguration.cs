using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Ignore(token => token.IsRevoked);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(token => token.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => new { token.MemberId, token.RevokedAt });
    }
}
