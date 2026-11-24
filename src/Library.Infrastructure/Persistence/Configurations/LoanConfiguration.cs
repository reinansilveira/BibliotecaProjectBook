using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasKey(loan => loan.Id);
        builder.Ignore(loan => loan.IsReturned);

        builder.Property(loan => loan.Format).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        builder.Property(loan => loan.ReadingLocation).HasMaxLength(500);
        builder.Property(loan => loan.ReadingProgress).HasPrecision(5, 2);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(loan => loan.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(loan => loan.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BookCopy>()
            .WithMany()
            .HasForeignKey(loan => loan.CopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(loan => loan.CopyId)
            .IsUnique()
            .HasDatabaseName("UX_Loans_CopyId_Active")
            .HasFilter("[CopyId] IS NOT NULL AND [ReturnedAt] IS NULL");
        builder.HasIndex(loan => new { loan.MemberId, loan.ReturnedAt });
        builder.HasIndex(loan => new { loan.BookId, loan.Format, loan.ReturnedAt });
        builder.HasIndex(loan => loan.DueDate).IncludeProperties(loan => loan.ReturnedAt);
        builder.HasIndex(loan => loan.LoanDate).IncludeProperties(loan => loan.BookId);
    }
}
