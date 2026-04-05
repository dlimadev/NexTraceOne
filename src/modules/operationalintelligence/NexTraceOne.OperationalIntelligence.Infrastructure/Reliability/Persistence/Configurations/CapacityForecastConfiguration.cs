using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade CapacityForecast.</summary>
internal sealed class CapacityForecastConfiguration
    : IEntityTypeConfiguration<CapacityForecast>
{
    public void Configure(EntityTypeBuilder<CapacityForecast> builder)
    {
        builder.ToTable("ops_reliability_capacity_forecasts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CapacityForecastId.From(value));

        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CurrentUtilizationPercent).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.GrowthRatePercentPerDay).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.EstimatedDaysToSaturation);
        builder.Property(x => x.SaturationRisk).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ComputedAt).IsRequired();

        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.Environment);
        builder.HasIndex(x => x.SaturationRisk);
    }
}
