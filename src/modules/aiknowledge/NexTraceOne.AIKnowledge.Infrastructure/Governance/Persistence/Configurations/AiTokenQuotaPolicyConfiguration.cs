using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiTokenQuotaPolicyConfiguration : IEntityTypeConfiguration<AiTokenQuotaPolicy>
{
    public void Configure(EntityTypeBuilder<AiTokenQuotaPolicy> builder)
    {
        builder.ToTable("AiTokenQuotaPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiTokenQuotaPolicyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScopeValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(200);
        builder.Property(x => x.ModelId).HasMaxLength(200);

        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.ScopeValue);
        builder.HasIndex(x => x.IsEnabled);
    }
}
