using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIKnowledgeSourceConfiguration : IEntityTypeConfiguration<AIKnowledgeSource>
{
    public void Configure(EntityTypeBuilder<AIKnowledgeSource> builder)
    {
        builder.ToTable("ai_gov_knowledge_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIKnowledgeSourceId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.EndpointOrPath).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.IsActive);
    }
}
