using Lloka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lloka.Infrastructure.Persistence.Configurations;

public class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> builder)
    {
        builder.ToTable("KycVerifications");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Status).HasConversion<string>().IsRequired();
        builder.Property(k => k.ExtractedFirstName).HasMaxLength(100);
        builder.Property(k => k.ExtractedLastName).HasMaxLength(100);
        builder.Property(k => k.DocumentNumber).HasMaxLength(50);
        builder.Property(k => k.SubmittedAt).IsRequired();
    }
}
