using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CostAttribution"/>.</summary>
internal sealed class CostAttributionConfiguration : IEntityTypeConfiguration<CostAttribution>
{
    public void Configure(EntityTypeBuilder<CostAttribution> builder)
    {
        builder.ToTable("oi_cost_attributions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CostAttributionId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.PeriodStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.TotalCost).IsRequired();
        builder.Property(x => x.RequestCount).IsRequired();
        builder.Property(x => x.CostPerRequest).IsRequired();

        builder.HasIndex(x => new { x.ApiAssetId, x.Environment, x.PeriodStart, x.PeriodEnd }).IsUnique();
        builder.HasIndex(x => x.ServiceName);
    }
}
