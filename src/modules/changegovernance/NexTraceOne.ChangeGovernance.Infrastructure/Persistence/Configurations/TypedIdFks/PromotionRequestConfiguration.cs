using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class PromotionRequestConfiguration : IEntityTypeConfiguration<PromotionRequest>
{
    public void Configure(EntityTypeBuilder<PromotionRequest> builder)
    {
        builder.Property(x => x.SourceEnvironmentId)
            .HasConversion(id => id.Value, value => new DeploymentEnvironmentId(value));
        builder.HasIndex(x => x.SourceEnvironmentId);
        builder.Property(x => x.TargetEnvironmentId)
            .HasConversion(id => id.Value, value => new DeploymentEnvironmentId(value));
        builder.HasIndex(x => x.TargetEnvironmentId);
    }
}
