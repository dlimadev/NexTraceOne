using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade LicenseComplianceReport.
/// Define mapeamento de tabela, typed ID, enums, colunas JSONB, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class LicenseComplianceReportConfiguration : IEntityTypeConfiguration<LicenseComplianceReport>
{
    public void Configure(EntityTypeBuilder<LicenseComplianceReport> builder)
    {
        builder.ToTable("gov_license_compliance_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new LicenseComplianceReportId(value));

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ScopeLabel)
            .HasMaxLength(300);

        builder.Property(x => x.TotalDependencies).IsRequired();
        builder.Property(x => x.CompliantCount).IsRequired();
        builder.Property(x => x.NonCompliantCount).IsRequired();
        builder.Property(x => x.WarningCount).IsRequired();

        builder.Property(x => x.OverallRiskLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CompliancePercent).IsRequired();

        // JSONB columns
        builder.Property(x => x.LicenseDetails).HasColumnType("jsonb");
        builder.Property(x => x.Conflicts).HasColumnType("jsonb");
        builder.Property(x => x.Recommendations).HasColumnType("jsonb");

        builder.Property(x => x.ScannedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        // Optimistic concurrency via PostgreSQL xmin
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes for common queries
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.ScopeKey);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ScannedAt);
        builder.HasIndex(x => x.OverallRiskLevel);
        builder.HasIndex(x => x.CompliancePercent);
        builder.HasIndex(x => new { x.Scope, x.ScopeKey, x.ScannedAt });
    }
}
