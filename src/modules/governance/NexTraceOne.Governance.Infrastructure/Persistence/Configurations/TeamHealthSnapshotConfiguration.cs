using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade TeamHealthSnapshot.
/// Define mapeamento de tabela, typed ID, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class TeamHealthSnapshotConfiguration : IEntityTypeConfiguration<TeamHealthSnapshot>
{
    public void Configure(EntityTypeBuilder<TeamHealthSnapshot> builder)
    {
        builder.ToTable("gov_team_health_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TeamHealthSnapshotId(value));

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.TeamName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.OverallScore)
            .IsRequired();

        builder.Property(x => x.ServiceCountScore).IsRequired();
        builder.Property(x => x.ContractHealthScore).IsRequired();
        builder.Property(x => x.IncidentFrequencyScore).IsRequired();
        builder.Property(x => x.MttrScore).IsRequired();
        builder.Property(x => x.TechDebtScore).IsRequired();
        builder.Property(x => x.DocCoverageScore).IsRequired();
        builder.Property(x => x.PolicyComplianceScore).IsRequired();

        builder.Property(x => x.DimensionDetails)
            .HasColumnType("jsonb");

        builder.Property(x => x.AssessedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.TeamId).IsUnique();
        builder.HasIndex(x => x.OverallScore);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AssessedAt);
    }
}
