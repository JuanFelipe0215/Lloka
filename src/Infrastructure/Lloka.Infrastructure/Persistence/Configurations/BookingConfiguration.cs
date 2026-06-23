using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lloka.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalAmount).HasPrecision(18, 2);
        builder.Property(b => b.GuestCount).IsRequired();

        builder.Property(b => b.Status).HasConversion<string>().IsRequired();

        builder.OwnsOne(b => b.StayPeriod, sp =>
        {
            // Npgsql exige offset 0 (UTC) para timestamptz. El dominio usa -05:00 (Colombia).
            // toDb: convierte a UTC antes de Npgsql. fromDb: devuelve UTC; el ctor privado aplica ToOffset(-5).
            var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
                v => v.ToUniversalTime(),
                v => v);

            sp.Property(x => x.CheckIn)
              .HasColumnName("CheckIn")
              .HasColumnType("timestamp with time zone")
              .HasConversion(utcConverter);
            sp.Property(x => x.CheckOut)
              .HasColumnName("CheckOut")
              .HasColumnType("timestamp with time zone")
              .HasConversion(utcConverter);
            sp.Ignore(x => x.Nights);
        });
    }
}
