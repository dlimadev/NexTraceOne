using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations;

internal sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("ai_orch_conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiConversationId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Topic).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TurnCount).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.StartedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ReleaseId);
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastTurnAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.Summary).HasMaxLength(10000);

        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedBy);
        builder.HasIndex(x => x.StartedAt);
    }
}
