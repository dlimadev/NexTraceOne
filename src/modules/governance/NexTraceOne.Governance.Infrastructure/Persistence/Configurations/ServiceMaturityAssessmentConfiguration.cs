using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ServiceMaturityAssessment.
/// Define mapeamento de tabela, typed ID, enums, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class ServiceMaturityAssessmentConfiguration : IEntityTypeConfiguration<ServiceMaturityAssessment>
{
    public void Configure(EntityTypeBuilder<ServiceMaturityAssessment> builder)
    {
        builder.ToTable("gov_service_maturity_assessments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceMaturityAssessmentId(value));

        builder.Property(x => x.ServiceId)
            .IsRequired();

        builder.Property(x => x.ServiceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CurrentLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Critérios booleanos
        builder.Property(x => x.OwnershipDefined).IsRequired();
        builder.Property(x => x.ContractsPublished).IsRequired();
        builder.Property(x => x.DocumentationExists).IsRequired();
        builder.Property(x => x.PoliciesApplied).IsRequired();
        builder.Property(x => x.ApprovalWorkflowActive).IsRequired();
        builder.Property(x => x.TelemetryActive).IsRequired();
        builder.Property(x => x.BaselinesEstablished).IsRequired();
        builder.Property(x => x.AlertsConfigured).IsRequired();
        builder.Property(x => x.RunbooksAvailable).IsRequired();
        builder.Property(x => x.RollbackTested).IsRequired();
        builder.Property(x => x.ChaosValidated).IsRequired();

        builder.Property(x => x.AssessedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.AssessedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100);

        builder.Property(x => x.LastReassessedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ReassessmentCount)
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.ServiceId).IsUnique();
        builder.HasIndex(x => x.CurrentLevel);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AssessedAt);
    }
}
