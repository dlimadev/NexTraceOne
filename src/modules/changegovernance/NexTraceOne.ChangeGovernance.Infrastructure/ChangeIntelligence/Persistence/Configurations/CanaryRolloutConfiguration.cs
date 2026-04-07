using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade CanaryRollout.</summary>
internal sealed class CanaryRolloutConfiguration : IEntityTypeConfiguration<CanaryRollout>
{
    /// <summary>Configura o mapeamento da entidade CanaryRollout para a tabela chg_canary_rollouts.</summary>
    public void Configure(EntityTypeBuilder<CanaryRollout> builder)
    {
        builder.ToTable("chg_canary_rollouts", t =>
        {
            t.HasCheckConstraint(
                "CK_chg_canary_rollouts_rollout_percentage",
                "\"RolloutPercentage\" >= 0 AND \"RolloutPercentage\" <= 100");
            t.HasCheckConstraint(
                "CK_chg_canary_rollouts_active_instances",
                "\"ActiveInstances\" >= 0");
            t.HasCheckConstraint(
                "CK_chg_canary_rollouts_total_instances",
                "\"TotalInstances\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CanaryRolloutId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.RolloutPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.ActiveInstances).IsRequired();
        builder.Property(x => x.TotalInstances).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsPromoted).IsRequired();
        builder.Property(x => x.IsAborted).IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ReleaseId);
    }
}
