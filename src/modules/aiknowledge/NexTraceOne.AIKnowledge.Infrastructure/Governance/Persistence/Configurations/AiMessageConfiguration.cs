using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AiGovernance.Domain.Entities;

namespace NexTraceOne.AiGovernance.Infrastructure.Persistence.Configurations;

internal sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("ai_gov_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiMessageId.From(value));

        builder.Property(x => x.Role).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200);
        builder.Property(x => x.Provider).HasMaxLength(200);
        builder.Property(x => x.AppliedPolicyName).HasMaxLength(200);
        builder.Property(x => x.GroundingSources).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ContextReferences).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Timestamp).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.CorrelationId);
    }
}
