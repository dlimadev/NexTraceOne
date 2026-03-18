using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ServiceCostProfile"/>.</summary>
internal sealed class ServiceCostProfileConfiguration : IEntityTypeConfiguration<ServiceCostProfile>
{
    public void Configure(EntityTypeBuilder<ServiceCostProfile> builder)
    {
        builder.ToTable("oi_service_cost_profiles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceCostProfileId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.MonthlyBudget);
        builder.Property(x => x.CurrentMonthCost).IsRequired();
        builder.Property(x => x.AlertThresholdPercent).IsRequired();

        builder.Property(x => x.LastUpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.ServiceName, x.Environment }).IsUnique();
    }
}
