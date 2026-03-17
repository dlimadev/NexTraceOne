using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiAssistantConversationConfiguration : IEntityTypeConfiguration<AiAssistantConversation>
{
    public void Configure(EntityTypeBuilder<AiAssistantConversation> builder)
    {
        builder.ToTable("ai_gov_conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiAssistantConversationId.From(value));

        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.DefaultContextScope).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.LastModelUsed).HasMaxLength(200);
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Tags).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.LastMessageAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.CreatedBy);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.LastMessageAt);
    }
}
