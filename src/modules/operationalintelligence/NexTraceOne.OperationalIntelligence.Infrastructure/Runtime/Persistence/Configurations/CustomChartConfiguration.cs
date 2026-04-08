using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CustomChart"/>.</summary>
internal sealed class CustomChartConfiguration : IEntityTypeConfiguration<CustomChart>
{
    public void Configure(EntityTypeBuilder<CustomChart> builder)
    {
        builder.ToTable("ops_custom_charts", t =>
        {
            t.HasCheckConstraint("CK_ops_custom_charts_chart_type",
                "\"ChartType\" >= 0 AND \"ChartType\" <= 6");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CustomChartId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ChartType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.MetricQuery).IsRequired();
        builder.Property(x => x.TimeRange).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FiltersJson);
        builder.Property(x => x.IsShared).IsRequired();

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.UserId, x.TenantId });
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsShared);

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
