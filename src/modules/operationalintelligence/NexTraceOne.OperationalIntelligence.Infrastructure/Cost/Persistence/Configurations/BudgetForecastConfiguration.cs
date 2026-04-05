using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

internal sealed class BudgetForecastConfiguration : IEntityTypeConfiguration<BudgetForecast>
{
    public void Configure(EntityTypeBuilder<BudgetForecast> builder)
    {
        builder.ToTable("ops_cost_budget_forecasts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BudgetForecastId.From(value));

        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ForecastPeriod).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ForecastNotes).HasMaxLength(1000);

        builder.Property(x => x.ProjectedCost).IsRequired();
        builder.Property(x => x.BudgetLimit);
        builder.Property(x => x.ConfidencePercent).IsRequired();
        builder.Property(x => x.IsOverBudgetProjected).IsRequired();

        builder.Property(x => x.ComputedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.ServiceId, x.Environment });
        builder.HasIndex(x => x.ComputedAt);
    }
}
