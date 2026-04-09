using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

internal sealed class ChangeCostImpactConfiguration : IEntityTypeConfiguration<ChangeCostImpact>
{
    public void Configure(EntityTypeBuilder<ChangeCostImpact> builder)
    {
        builder.ToTable("gov_change_cost_impacts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ChangeCostImpactId(value));

        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeDescription).HasMaxLength(500);

        builder.Property(x => x.BaselineCostPerDay).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.ActualCostPerDay).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CostDelta).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CostDeltaPercentage).HasColumnType("numeric(18,4)").IsRequired();

        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CostProvider).HasMaxLength(100);
        builder.Property(x => x.CostDetails).HasColumnType("jsonb");

        builder.Property(x => x.MeasurementWindowStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.MeasurementWindowEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(100);

        // Optimistic concurrency via PostgreSQL xmin
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RecordedAt);
        builder.HasIndex(x => x.CostDelta);
    }
}
