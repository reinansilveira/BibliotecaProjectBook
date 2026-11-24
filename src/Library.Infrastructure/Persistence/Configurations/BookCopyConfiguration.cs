using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");
        builder.HasKey(copy => copy.Id);
        builder.Ignore(copy => copy.IsWithdrawn);

        builder.Property(copy => copy.Barcode).HasMaxLength(20).IsUnicode(false).IsRequired();

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(copy => copy.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(copy => copy.Barcode).IsUnique();
        builder.HasIndex(copy => new { copy.BookId, copy.WithdrawnAt });
    }
}
