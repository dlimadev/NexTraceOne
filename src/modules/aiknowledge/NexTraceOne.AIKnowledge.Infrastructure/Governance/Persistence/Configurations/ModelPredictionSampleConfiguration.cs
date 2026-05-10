using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class ModelPredictionSampleConfiguration : IEntityTypeConfiguration<ModelPredictionSample>
{
    public void Configure(EntityTypeBuilder<ModelPredictionSample> builder)
    {
        builder.ToTable("aik_model_prediction_samples");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ModelPredictionSampleId.From(value));

        builder.Property(x => x.ModelId).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PredictedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.InputFeatureStatsJson).HasColumnType("jsonb");
        builder.Property(x => x.PredictedClass).HasMaxLength(200);
        builder.Property(x => x.ConfidenceScore);
        builder.Property(x => x.InferenceLatencyMs);
        builder.Property(x => x.ActualClass).HasMaxLength(200);
        builder.Property(x => x.IsFallback).IsRequired();
        builder.Property(x => x.DriftAcknowledged).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.PredictedAt });
        builder.HasIndex(x => new { x.ModelId, x.TenantId });
    }
}
