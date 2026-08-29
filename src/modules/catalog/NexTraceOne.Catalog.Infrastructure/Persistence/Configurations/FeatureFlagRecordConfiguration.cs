using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class FeatureFlagRecordConfiguration : IEntityTypeConfiguration<FeatureFlagRecord>
{
    public void Configure(EntityTypeBuilder<FeatureFlagRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FlagKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerId).HasMaxLength(200);

        // Garante o upsert idempotente por (TenantId, ServiceId, FlagKey)
        // documentado no contrato de ingestão de flags.
        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.FlagKey })
            .IsUnique();
    }
}
