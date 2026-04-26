using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para DashboardRevision (V3.1 — Dashboard Intelligence Foundation).
/// Snapshots imutáveis de CustomDashboard para histórico, diff e revert.
/// </summary>
internal sealed class DashboardRevisionConfiguration : IEntityTypeConfiguration<DashboardRevision>
{
    public void Configure(EntityTypeBuilder<DashboardRevision> builder)
    {
        builder.ToTable("gov_dashboard_revisions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DashboardRevisionId(value));

        builder.Property(x => x.DashboardId)
            .HasConversion(id => id.Value, value => new CustomDashboardId(value))
            .IsRequired();

        builder.Property(x => x.RevisionNumber)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Layout)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.WidgetsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.VariablesJson)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.AuthorUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ChangeNote)
            .HasMaxLength(500);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Índices: buscas frequentes por dashboard + tenant, ordenadas por número de revisão
        builder.HasIndex(x => new { x.DashboardId, x.TenantId })
            .HasDatabaseName("ix_gov_dashboard_revisions_dashboard_tenant");

        builder.HasIndex(x => new { x.DashboardId, x.RevisionNumber })
            .HasDatabaseName("ix_gov_dashboard_revisions_dashboard_number");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gov_dashboard_revisions_tenant");
    }
}
