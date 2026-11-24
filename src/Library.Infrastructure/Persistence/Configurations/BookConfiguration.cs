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

        builder.Property(book => book.Title).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(150).IsRequired();
        builder.Property(book => book.Description).HasMaxLength(1000);

        builder.HasIndex(book => book.Title).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(book => book.Author).HasFilter("[IsDeleted] = 0");
    }
}
