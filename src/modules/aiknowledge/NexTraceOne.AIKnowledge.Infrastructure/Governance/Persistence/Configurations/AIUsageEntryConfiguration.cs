using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIUsageEntryConfiguration : IEntityTypeConfiguration<AIUsageEntry>
{
    public void Configure(EntityTypeBuilder<AIUsageEntry> builder)
    {
        builder.ToTable("aik_usage_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIUsageEntryId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UserDisplayName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Timestamp).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PolicyName).HasMaxLength(200);
        builder.Property(x => x.Result).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.ContextScope).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ClientType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.ModelId);
        builder.HasIndex(x => x.CorrelationId);
    }
}
