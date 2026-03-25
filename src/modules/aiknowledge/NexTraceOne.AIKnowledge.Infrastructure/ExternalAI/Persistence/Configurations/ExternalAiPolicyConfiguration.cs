using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Configurations;

internal sealed class ExternalAiPolicyConfiguration : IEntityTypeConfiguration<ExternalAiPolicy>
{
    public void Configure(EntityTypeBuilder<ExternalAiPolicy> builder)
    {
        builder.ToTable("aik_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalAiPolicyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.MaxDailyQueries).IsRequired();
        builder.Property(x => x.MaxTokensPerDay).IsRequired();
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.AllowedContexts).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
