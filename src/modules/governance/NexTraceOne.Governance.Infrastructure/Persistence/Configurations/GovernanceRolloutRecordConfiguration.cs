using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade GovernanceRolloutRecord.
/// Define mapeamento de tabela, typed IDs, enums e índices.
/// </summary>
internal sealed class GovernanceRolloutRecordConfiguration : IEntityTypeConfiguration<GovernanceRolloutRecord>
{
    public void Configure(EntityTypeBuilder<GovernanceRolloutRecord> builder)
    {
        builder.ToTable("gov_rollout_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GovernanceRolloutRecordId(value));

        builder.Property(x => x.PackId)
            .HasConversion(id => id.Value, value => new GovernancePackId(value))
            .IsRequired();

        builder.Property(x => x.VersionId)
            .HasConversion(id => id.Value, value => new GovernancePackVersionId(value))
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ScopeType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EnforcementMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.InitiatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.InitiatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.PackId);
        builder.HasIndex(x => x.VersionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.InitiatedAt);
        builder.HasIndex(x => x.CompletedAt);
    }
}
