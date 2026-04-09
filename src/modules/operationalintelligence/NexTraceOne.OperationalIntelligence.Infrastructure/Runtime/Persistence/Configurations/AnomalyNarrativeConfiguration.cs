using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade AnomalyNarrative.</summary>
internal sealed class AnomalyNarrativeConfiguration : IEntityTypeConfiguration<AnomalyNarrative>
{
    public void Configure(EntityTypeBuilder<AnomalyNarrative> builder)
    {
        builder.ToTable("ops_anomaly_narratives");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AnomalyNarrativeId.From(value));

        builder.Property(x => x.DriftFindingId)
            .HasConversion(id => id.Value, value => DriftFindingId.From(value))
            .IsRequired();

        builder.Property(x => x.NarrativeText).IsRequired();
        builder.Property(x => x.SymptomsSection).HasMaxLength(10000);
        builder.Property(x => x.BaselineComparisonSection).HasMaxLength(10000);
        builder.Property(x => x.ProbableCauseSection).HasMaxLength(10000);
        builder.Property(x => x.CorrelatedChangesSection).HasMaxLength(10000);
        builder.Property(x => x.RecommendedActionsSection).HasMaxLength(10000);
        builder.Property(x => x.SeverityJustificationSection).HasMaxLength(10000);
        builder.Property(x => x.ModelUsed).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TokensUsed).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.TenantId);
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastRefreshedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RefreshCount).IsRequired();

        builder.HasIndex(x => x.DriftFindingId).IsUnique();
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_anomaly_narratives_tenant_id");
    }
}
