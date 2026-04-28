using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class AlertFiringRecordConfiguration : IEntityTypeConfiguration<AlertFiringRecord>
{
    public void Configure(EntityTypeBuilder<AlertFiringRecord> builder)
    {
        builder.ToTable("iam_alert_firing_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AlertFiringRecordId.From(v))
            .HasColumnType("uuid");
        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.AlertRuleId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.AlertRuleName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConditionSummary).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.FiredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ResolvedReason).HasMaxLength(500);
        builder.Property(x => x.NotificationChannels).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.FiredAt })
            .HasDatabaseName("ix_iam_alert_firing_tenant_fired");
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_iam_alert_firing_tenant_status");
        builder.HasIndex(x => x.AlertRuleId).HasDatabaseName("ix_iam_alert_firing_rule");
    }
}
