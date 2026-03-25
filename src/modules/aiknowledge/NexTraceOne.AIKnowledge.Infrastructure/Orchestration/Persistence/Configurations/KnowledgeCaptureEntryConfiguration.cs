using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations;

internal sealed class KnowledgeCaptureEntryConfiguration : IEntityTypeConfiguration<KnowledgeCaptureEntry>
{
    public void Configure(EntityTypeBuilder<KnowledgeCaptureEntry> builder)
    {
        builder.ToTable("aik_knowledge_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => KnowledgeCaptureEntryId.From(value));

        builder.Property(x => x.ConversationId)
            .HasConversion(id => id.Value, value => AiConversationId.From(value))
            .IsRequired();

        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(50000).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Relevance).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.ValidatedBy).HasMaxLength(500);
        builder.Property(x => x.ValidatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.SuggestedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SuggestedAt);
    }
}
