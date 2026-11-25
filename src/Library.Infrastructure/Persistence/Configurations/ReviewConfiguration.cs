using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(review => review.Id);

        builder.Property(review => review.Comment).HasMaxLength(2000);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(review => review.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(review => review.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(review => new { review.BookId, review.MemberId }).IsUnique();
        builder.HasIndex(review => new { review.BookId, review.UpdatedAt });
    }
}
