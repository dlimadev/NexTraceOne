using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiFeatureModelBindingConfiguration : IEntityTypeConfiguration<AiFeatureModelBinding>
{
    public void Configure(EntityTypeBuilder<AiFeatureModelBinding> builder)
    {
        builder.ToTable("aik_feature_model_bindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiFeatureModelBindingId.From(value));

        builder.Property(x => x.FeatureKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RequiredModelId).IsRequired();
        builder.Property(x => x.RequiredModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequiredProviderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FallbackModelId);
        builder.Property(x => x.FallbackModelName).HasMaxLength(200);
        builder.Property(x => x.FallbackProviderId).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();

        // Unique per feature key + tenant (only one binding per feature per tenant)
        builder.HasIndex(x => new { x.FeatureKey, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsActive);
    }
}
