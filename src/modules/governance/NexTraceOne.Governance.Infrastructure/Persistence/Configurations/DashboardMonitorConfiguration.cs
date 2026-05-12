using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

public sealed class DashboardMonitorConfiguration : IEntityTypeConfiguration<DashboardMonitorDefinition>
{
    public void Configure(EntityTypeBuilder<DashboardMonitorDefinition> builder)
    {
        builder.ToTable("gov_dashboard_monitors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => new DashboardMonitorDefinitionId(v));
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DashboardId).IsRequired();
        builder.Property(x => x.WidgetId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NqlQuery).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ConditionField).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConditionOperator).IsRequired();
        builder.Property(x => x.ConditionThreshold).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.EvaluationWindowMinutes).IsRequired();
        builder.Property(x => x.Severity).IsRequired();
        builder.Property(x => x.NotificationChannelsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.LastFiredAt);
        builder.Property(x => x.FiredCount).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(new[] { "DashboardId", "TenantId" })
            .HasDatabaseName("ix_gov_monitor_dashboard_tenant");
        builder.HasIndex(new[] { "TenantId", "Status" })
            .HasDatabaseName("ix_gov_monitor_tenant_status");
    }
}
