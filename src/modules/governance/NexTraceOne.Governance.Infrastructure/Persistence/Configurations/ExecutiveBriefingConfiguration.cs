using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ExecutiveBriefing.
/// Define mapeamento de tabela, typed ID, enums, colunas JSONB, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class ExecutiveBriefingConfiguration : IEntityTypeConfiguration<ExecutiveBriefing>
{
    public void Configure(EntityTypeBuilder<ExecutiveBriefing> builder)
    {
        builder.ToTable("gov_executive_briefings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ExecutiveBriefingId(value));

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Frequency)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExecutiveSummary);

        // Secções JSONB
        builder.Property(x => x.PlatformStatusSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.TopIncidentsSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.TeamPerformanceSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.HighRiskChangesSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.ComplianceStatusSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.CostTrendsSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.ActiveRisksSection)
            .HasColumnType("jsonb");

        builder.Property(x => x.GeneratedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.GeneratedByAgent)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PublishedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ArchivedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Frequency);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.GeneratedAt);
    }
}
