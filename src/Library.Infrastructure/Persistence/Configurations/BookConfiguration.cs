using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(book => book.Id);
        builder.Ignore(book => book.HasEbook);

        builder.Property(book => book.Title).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(150).IsRequired();
        builder.Property(book => book.Description).HasMaxLength(2000);
        builder.Property(book => book.Genre).HasMaxLength(50).IsRequired();
        builder.Property(book => book.Isbn).HasMaxLength(13).IsUnicode(false);
        builder.Property(book => book.CoverUrl).HasMaxLength(500);
        builder.Property(book => book.EbookFileKey).HasMaxLength(200);
        builder.Property(book => book.AverageRating).HasPrecision(3, 2);

        builder.HasIndex(book => book.Title).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(book => book.Author).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(book => book.Genre).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(book => book.Isbn).IsUnique().HasFilter("[Isbn] IS NOT NULL AND [IsDeleted] = 0");
    }
}
