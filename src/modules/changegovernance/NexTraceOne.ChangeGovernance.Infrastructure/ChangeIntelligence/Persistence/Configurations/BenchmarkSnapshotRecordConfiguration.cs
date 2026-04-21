using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade BenchmarkSnapshotRecord.
/// Tabela: chg_benchmark_snapshots
/// </summary>
internal sealed class BenchmarkSnapshotRecordConfiguration : IEntityTypeConfiguration<BenchmarkSnapshotRecord>
{
    /// <summary>Configura o mapeamento da entidade BenchmarkSnapshotRecord.</summary>
    public void Configure(EntityTypeBuilder<BenchmarkSnapshotRecord> builder)
    {
        builder.ToTable("chg_benchmark_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BenchmarkSnapshotRecordId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DeploymentFrequencyPerWeek).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.LeadTimeForChangesHours).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.ChangeFailureRatePercent).HasPrecision(7, 4).IsRequired();
        builder.Property(x => x.MeanTimeToRestoreHours).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.MaturityScore).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.CostPerRequestUsd).HasPrecision(18, 8);
        builder.Property(x => x.ServiceCount).IsRequired();
        builder.Property(x => x.IsAnonymizedForBenchmarks).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_benchmark_snapshots_tenant_id");
        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).HasDatabaseName("ix_chg_benchmark_snapshots_period");
        builder.HasIndex(x => x.IsAnonymizedForBenchmarks)
            .HasDatabaseName("ix_chg_benchmark_snapshots_anonymized")
            .HasFilter("\"IsAnonymizedForBenchmarks\" = true");
    }
}
