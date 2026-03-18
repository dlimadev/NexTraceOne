using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations;

internal sealed class GeneratedTestArtifactConfiguration : IEntityTypeConfiguration<GeneratedTestArtifact>
{
    public void Configure(EntityTypeBuilder<GeneratedTestArtifact> builder)
    {
        builder.ToTable("ai_orch_test_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => GeneratedTestArtifactId.From(value));

        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TestFramework).HasMaxLength(100).IsRequired();
        builder.Property(x => x.GeneratedCode).HasColumnType("text").IsRequired();
        builder.Property(x => x.Confidence).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(500);
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.GeneratedAt);
    }
}
