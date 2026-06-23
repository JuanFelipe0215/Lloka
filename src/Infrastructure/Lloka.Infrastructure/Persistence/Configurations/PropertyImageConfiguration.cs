using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lloka.Infrastructure.Persistence.Configurations;

public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.ToTable("PropertyImages");
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Url).HasMaxLength(2000).IsRequired();
        builder.Property(pi => pi.DisplayOrder).IsRequired();
    }
}
