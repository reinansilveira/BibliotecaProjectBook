using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(member => member.Id);

        builder.Property(member => member.Name).HasMaxLength(150).IsRequired();
        builder.Property(member => member.Phone).HasMaxLength(20).IsRequired();
        builder.Property(member => member.Email).HasMaxLength(254).IsRequired();
        builder.Property(member => member.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(member => member.Email).IsUnique();
        builder.HasIndex(member => member.Name).HasFilter("[IsDeleted] = 0");
    }
}
