using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Ignore(reservation => reservation.IsOpen);

        builder.Property(reservation => reservation.Format).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        builder.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(reservation => reservation.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(reservation => reservation.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => new { reservation.BookId, reservation.MemberId, reservation.Format })
            .IsUnique()
            .HasDatabaseName("UX_Reservations_Open")
            .HasFilter("[Status] IN ('Waiting', 'Ready')");
        builder.HasIndex(reservation => new { reservation.BookId, reservation.Format, reservation.Status, reservation.CreatedAt });
        builder.HasIndex(reservation => new { reservation.MemberId, reservation.Status });
        builder.HasIndex(reservation => reservation.ExpiresAt).HasFilter("[Status] = 'Ready'");
    }
}
