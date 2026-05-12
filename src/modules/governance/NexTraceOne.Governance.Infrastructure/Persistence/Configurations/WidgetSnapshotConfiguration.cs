using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade WidgetSnapshot.</summary>
internal sealed class WidgetSnapshotConfiguration : IEntityTypeConfiguration<WidgetSnapshot>
{
    public void Configure(EntityTypeBuilder<WidgetSnapshot> builder)
    {
        builder.ToTable("gov_widget_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new WidgetSnapshotId(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DashboardId).IsRequired();
        builder.Property(x => x.WidgetId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DataHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(new[] { "TenantId", "DashboardId", "WidgetId", "CapturedAt" })
            .HasDatabaseName("ix_gov_widget_snapshot_lookup");
        builder.HasIndex(new[] { "TenantId", "DashboardId", "WidgetId" })
            .HasDatabaseName("ix_gov_widget_snapshot_widget");
    }
}