using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core de FeatureFlagRecord.
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
internal sealed class FeatureFlagRecordConfiguration : IEntityTypeConfiguration<FeatureFlagRecord>
{
    public void Configure(EntityTypeBuilder<FeatureFlagRecord> builder)
    {
        builder.ToTable("ctr_feature_flag_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FlagKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.EnabledEnvironmentsJson).HasColumnType("jsonb");
        builder.Property(x => x.OwnerId).HasMaxLength(200);
        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.LastToggledAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.ScheduledRemovalDate)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ServiceId });
        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.FlagKey }).IsUnique();
    }
}
