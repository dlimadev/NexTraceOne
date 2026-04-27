using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para ScheduledDashboardReport (V3.6).</summary>
internal sealed class ScheduledDashboardReportConfiguration : IEntityTypeConfiguration<ScheduledDashboardReport>
{
    public void Configure(EntityTypeBuilder<ScheduledDashboardReport> builder)
    {
        builder.ToTable("gov_scheduled_dashboard_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ScheduledDashboardReportId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CronExpression).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).HasDefaultValue("pdf");
        builder.Property(x => x.RecipientsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.WebhookUrl).HasMaxLength(500);
        builder.Property(x => x.RetentionDays).HasDefaultValue(90);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.LastFailureMessage).HasMaxLength(1000);

        builder.HasIndex(x => new { x.DashboardId, x.TenantId })
            .HasDatabaseName("ix_gov_sched_report_dashboard_tenant");

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("ix_gov_sched_report_tenant_active");

        builder.HasIndex(x => x.NextRunAt)
            .HasDatabaseName("ix_gov_sched_report_next_run");
    }
}
