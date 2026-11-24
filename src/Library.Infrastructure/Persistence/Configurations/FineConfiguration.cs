using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fines");
        builder.HasKey(fine => fine.Id);

        builder.Property(fine => fine.Amount).HasPrecision(10, 2);
        builder.Property(fine => fine.DailyRate).HasPrecision(10, 2);
        builder.Property(fine => fine.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        builder.Property(fine => fine.PaymentReference).HasMaxLength(40);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(fine => fine.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Loan>()
            .WithMany()
            .HasForeignKey(fine => fine.LoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(fine => fine.LoanId).IsUnique();
        builder.HasIndex(fine => new { fine.MemberId, fine.Status }).IncludeProperties(fine => fine.Amount);
    }
}
