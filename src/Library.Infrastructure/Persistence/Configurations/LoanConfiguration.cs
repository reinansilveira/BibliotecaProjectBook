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

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(loan => loan.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(loan => loan.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(loan => new { loan.MemberId, loan.ReturnedAt });
        builder.HasIndex(loan => new { loan.BookId, loan.ReturnedAt });
        builder.HasIndex(loan => loan.DueDate).IncludeProperties(loan => loan.ReturnedAt);
        builder.HasIndex(loan => loan.LoanDate).IncludeProperties(loan => loan.BookId);
    }
}
