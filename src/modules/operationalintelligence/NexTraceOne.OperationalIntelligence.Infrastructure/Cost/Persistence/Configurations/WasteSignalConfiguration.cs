using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

internal sealed class WasteSignalConfiguration : IEntityTypeConfiguration<WasteSignal>
{
    public void Configure(EntityTypeBuilder<WasteSignal> builder)
    {
        builder.ToTable("oi_waste_signals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => WasteSignalId.From(v))
            .HasColumnName("id");

        builder.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasColumnName("environment").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SignalType).HasColumnName("signal_type").HasMaxLength(50).IsRequired()
            .HasConversion<string>();
        builder.Property(x => x.EstimatedMonthlySavings).HasColumnName("estimated_monthly_savings").HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TeamName).HasColumnName("team_name").HasMaxLength(200);
        builder.Property(x => x.IsAcknowledged).HasColumnName("is_acknowledged");
        builder.Property(x => x.AcknowledgedAt).HasColumnName("acknowledged_at");
        builder.Property(x => x.AcknowledgedBy).HasColumnName("acknowledged_by").HasMaxLength(200);
        builder.Property(x => x.DetectedAt).HasColumnName("detected_at");

        builder.HasIndex(x => x.ServiceName).HasDatabaseName("ix_oi_waste_signals_service");
        builder.HasIndex(x => new { x.TeamName, x.IsAcknowledged }).HasDatabaseName("ix_oi_waste_signals_team_ack");
    }
}
