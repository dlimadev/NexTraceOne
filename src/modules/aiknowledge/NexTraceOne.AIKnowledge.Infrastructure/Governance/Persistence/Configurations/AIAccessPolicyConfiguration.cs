using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIAccessPolicyConfiguration : IEntityTypeConfiguration<AIAccessPolicy>
{
    public void Configure(EntityTypeBuilder<AIAccessPolicy> builder)
    {
        builder.ToTable("aik_access_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIAccessPolicyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScopeValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AllowedModelIds).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.BlockedModelIds).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EnvironmentRestrictions).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DataRetentionDays);

        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.IsActive);
    }
}
