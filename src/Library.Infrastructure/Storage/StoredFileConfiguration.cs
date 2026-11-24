using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Storage;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");
        builder.HasKey(file => file.Key);

        builder.Property(file => file.Key).HasMaxLength(200).IsUnicode(false);
        builder.Property(file => file.FileName).HasMaxLength(200).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(file => file.Content).IsRequired();
    }
}
