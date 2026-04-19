using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

public sealed class ChangeConfidenceScoreConfiguration : IEntityTypeConfiguration<ChangeConfidenceScore>
{
    public void Configure(EntityTypeBuilder<ChangeConfidenceScore> builder)
    {
        builder.ToTable("aik_change_confidence_scores");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => ChangeConfidenceScoreId.From(value));

        builder.Property(e => e.ChangeId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Verdict).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CalculatedBy).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ScoreBreakdownJson).HasColumnType("text");
        builder.Property(e => e.RecommendationText).HasMaxLength(2000);

        builder.HasIndex(e => new { e.ChangeId, e.TenantId });
        builder.HasIndex(e => new { e.ServiceName, e.TenantId });
        builder.HasIndex(e => e.TenantId);
    }
}
