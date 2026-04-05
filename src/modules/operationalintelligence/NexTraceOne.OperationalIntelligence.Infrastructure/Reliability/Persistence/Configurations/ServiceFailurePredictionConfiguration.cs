using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade ServiceFailurePrediction.</summary>
internal sealed class ServiceFailurePredictionConfiguration
    : IEntityTypeConfiguration<ServiceFailurePrediction>
{
    public void Configure(EntityTypeBuilder<ServiceFailurePrediction> builder)
    {
        builder.ToTable("ops_reliability_failure_predictions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceFailurePredictionId.From(value));

        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FailureProbabilityPercent).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.RiskLevel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PredictionHorizon).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CausalFactors)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);
        builder.Property(x => x.RecommendedAction).HasMaxLength(1000);
        builder.Property(x => x.ComputedAt).IsRequired();

        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.Environment);
        builder.HasIndex(x => x.RiskLevel);
    }
}
