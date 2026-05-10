using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class ModelRoutingPolicyConfiguration : IEntityTypeConfiguration<ModelRoutingPolicy>
{
    public void Configure(EntityTypeBuilder<ModelRoutingPolicy> builder)
    {
        builder.ToTable("aik_model_routing_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ModelRoutingPolicyId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Intent).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.PreferredModelName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FallbackModelName).HasMaxLength(300);
        builder.Property(x => x.MaxTokens).IsRequired();
        builder.Property(x => x.MaxCostPerRequestUsd).HasColumnType("numeric(10,6)").IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Intent, x.IsActive });
    }
}
