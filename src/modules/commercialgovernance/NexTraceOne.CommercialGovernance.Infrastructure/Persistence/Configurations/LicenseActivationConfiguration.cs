using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("licensing_activations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LicenseActivationId.From(value));
        builder.Property(x => x.HardwareFingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActivatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActivatedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
    }
}
