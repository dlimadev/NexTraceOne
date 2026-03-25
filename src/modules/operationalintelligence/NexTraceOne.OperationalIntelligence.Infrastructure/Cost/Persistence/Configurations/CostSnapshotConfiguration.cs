using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CostSnapshot"/>.</summary>
internal sealed class CostSnapshotConfiguration : IEntityTypeConfiguration<CostSnapshot>
{
    public void Configure(EntityTypeBuilder<CostSnapshot> builder)
    {
        builder.ToTable("ops_cost_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CostSnapshotId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();

        builder.Property(x => x.TotalCost).IsRequired();
        builder.Property(x => x.CpuCostShare).IsRequired();
        builder.Property(x => x.MemoryCostShare).IsRequired();
        builder.Property(x => x.NetworkCostShare).IsRequired();
        builder.Property(x => x.StorageCostShare).IsRequired();

        builder.Property(x => x.CapturedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.CapturedAt });
        builder.HasIndex(x => x.Period);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
