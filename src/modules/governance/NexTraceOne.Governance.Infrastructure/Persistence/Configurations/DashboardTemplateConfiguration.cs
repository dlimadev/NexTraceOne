using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para DashboardTemplate (V3.8 — Marketplace &amp; Plugin SDK).</summary>
internal sealed class DashboardTemplateConfiguration : IEntityTypeConfiguration<DashboardTemplate>
{
    public void Configure(EntityTypeBuilder<DashboardTemplate> builder)
    {
        builder.ToTable("gov_dashboard_templates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DashboardTemplateId(value));

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(20).HasDefaultValue("1.0.0");
        builder.Property(x => x.TagsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.DashboardSnapshotJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RequiredVariablesJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.InstallCount).HasDefaultValue(0);
        builder.Property(x => x.IsSystem).HasDefaultValue(false);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100);

        builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.IsSystem, x.Category })
            .HasDatabaseName("ix_gov_dash_templates_system_category");

        builder.HasIndex(x => new { x.TenantId, x.Category })
            .HasDatabaseName("ix_gov_dash_templates_tenant_category");

        builder.HasIndex(x => x.Persona)
            .HasDatabaseName("ix_gov_dash_templates_persona");
    }
}
