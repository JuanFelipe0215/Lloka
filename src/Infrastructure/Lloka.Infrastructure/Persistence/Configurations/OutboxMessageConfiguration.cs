using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lloka.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Payload).IsRequired();

        builder.Property(o => o.Status).HasConversion<string>().IsRequired();

        builder.HasIndex(o => o.Status);
    }
}
