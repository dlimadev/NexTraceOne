using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

internal sealed class EfficiencyRecommendationConfiguration : IEntityTypeConfiguration<EfficiencyRecommendation>
{
    public void Configure(EntityTypeBuilder<EfficiencyRecommendation> builder)
    {
        builder.ToTable("ops_cost_efficiency_recommendations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EfficiencyRecommendationId.From(value));

        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RecommendationText).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(20).IsRequired();

        builder.Property(x => x.ServiceCost).IsRequired();
        builder.Property(x => x.MedianPeerCost).IsRequired();
        builder.Property(x => x.DeviationPercent).IsRequired();
        builder.Property(x => x.IsAcknowledged).IsRequired();

        builder.Property(x => x.GeneratedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.IsAcknowledged);
    }
}
