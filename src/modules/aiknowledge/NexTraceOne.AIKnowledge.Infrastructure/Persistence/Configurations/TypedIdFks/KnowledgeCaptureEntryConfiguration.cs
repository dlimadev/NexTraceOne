using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class KnowledgeCaptureEntryConfiguration : IEntityTypeConfiguration<KnowledgeCaptureEntry>
{
    public void Configure(EntityTypeBuilder<KnowledgeCaptureEntry> builder)
    {
        builder.Property(x => x.ConversationId)
            .HasConversion(id => id.Value, value => new AiConversationId(value));
        builder.HasIndex(x => x.ConversationId);
    }
}
