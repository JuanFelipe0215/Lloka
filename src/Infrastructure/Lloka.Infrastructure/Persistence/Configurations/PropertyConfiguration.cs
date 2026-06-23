using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lloka.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).IsRequired();
        builder.Property(p => p.PricePerNight).HasPrecision(18, 2);
        builder.Property(p => p.MaxGuests).IsRequired();

        builder.Property(p => p.Status).HasConversion<string>().IsRequired();

        builder.OwnsOne(p => p.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("Street").HasMaxLength(300).IsRequired();
            a.Property(x => x.City).HasColumnName("City").HasMaxLength(100).IsRequired();
            a.Property(x => x.Department).HasColumnName("Department").HasMaxLength(100).IsRequired();
            a.Property(x => x.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            a.Property(x => x.Latitude).HasColumnName("Latitude").HasPrecision(9, 6);
            a.Property(x => x.Longitude).HasColumnName("Longitude").HasPrecision(9, 6);
            a.Property(x => x.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
        });

        builder.HasMany(p => p.Images)
               .WithOne()
               .HasForeignKey(pi => pi.PropertyId);
    }
}
