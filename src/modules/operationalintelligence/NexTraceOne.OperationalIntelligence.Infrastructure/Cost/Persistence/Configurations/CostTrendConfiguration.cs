using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CostTrend"/>.</summary>
internal sealed class CostTrendConfiguration : IEntityTypeConfiguration<CostTrend>
{
    public void Configure(EntityTypeBuilder<CostTrend> builder)
    {
        builder.ToTable("oi_cost_trends");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CostTrendId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.PeriodStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.AverageDailyCost).IsRequired();
        builder.Property(x => x.PeakDailyCost).IsRequired();
        builder.Property(x => x.TrendDirection).HasColumnType("integer").IsRequired();
        builder.Property(x => x.PercentageChange).IsRequired();
        builder.Property(x => x.DataPointCount).IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment, x.PeriodStart, x.PeriodEnd }).IsUnique();
        builder.HasIndex(x => x.TrendDirection);
    }
}
