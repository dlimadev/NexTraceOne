using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class LicenseCapabilityConfiguration : IEntityTypeConfiguration<LicenseCapability>
{
    public void Configure(EntityTypeBuilder<LicenseCapability> builder)
    {
        builder.ToTable("licensing_capabilities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LicenseCapabilityId.From(value));
        builder.Property(x => x.Code).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
    }
}
