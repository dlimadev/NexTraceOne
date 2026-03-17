using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Infrastructure.Persistence.Configurations;

internal sealed class AIIDECapabilityPolicyConfiguration : IEntityTypeConfiguration<AIIDECapabilityPolicy>
{
    public void Configure(EntityTypeBuilder<AIIDECapabilityPolicy> builder)
    {
        builder.ToTable("ai_gov_ide_capability_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIIDECapabilityPolicyId.From(value));

        builder.Property(x => x.ClientType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(100);
        builder.Property(x => x.AllowedCommands).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AllowedContextScopes).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AllowedModelIds).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.ClientType);
        builder.HasIndex(x => x.IsActive);
    }
}
