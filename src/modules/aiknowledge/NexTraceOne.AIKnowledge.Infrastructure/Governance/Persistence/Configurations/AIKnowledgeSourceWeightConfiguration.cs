using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIKnowledgeSourceWeightConfiguration : IEntityTypeConfiguration<AIKnowledgeSourceWeight>
{
    public void Configure(EntityTypeBuilder<AIKnowledgeSourceWeight> builder)
    {
        builder.ToTable("aik_source_weights");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIKnowledgeSourceWeightId.From(value));

        builder.Property(x => x.SourceType)
            .HasMaxLength(100)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.UseCaseType)
            .HasMaxLength(100)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Relevance)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ConfiguredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.UseCaseType, x.SourceType });
        builder.HasIndex(x => x.IsActive);
    }
}
