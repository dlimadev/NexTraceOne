using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade TechnicalDebtItem.
/// Define mapeamento de tabela, typed ID, check constraints, concorrência otimista e índices.
/// </summary>
internal sealed class TechnicalDebtItemConfiguration : IEntityTypeConfiguration<TechnicalDebtItem>
{
    public void Configure(EntityTypeBuilder<TechnicalDebtItem> builder)
    {
        builder.ToTable("gov_technical_debt_items", t =>
        {
            t.HasCheckConstraint(
                "CK_gov_technical_debt_items_severity",
                "\"Severity\" IN ('critical', 'high', 'medium', 'low')");

            t.HasCheckConstraint(
                "CK_gov_technical_debt_items_debt_type",
                "\"DebtType\" IN ('architecture', 'code-quality', 'security', 'dependency', 'documentation', 'testing', 'performance', 'infrastructure')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TechnicalDebtItemId(value));

        builder.Property(x => x.ServiceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DebtType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EstimatedEffortDays)
            .IsRequired();

        builder.Property(x => x.DebtScore)
            .IsRequired();

        builder.Property(x => x.Tags)
            .HasMaxLength(500);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.DebtType);
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.CreatedAt);
    }
}
