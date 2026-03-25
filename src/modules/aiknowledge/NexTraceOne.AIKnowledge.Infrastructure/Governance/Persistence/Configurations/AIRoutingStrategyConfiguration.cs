using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIRoutingStrategyConfiguration : IEntityTypeConfiguration<AIRoutingStrategy>
{
    public void Configure(EntityTypeBuilder<AIRoutingStrategy> builder)
    {
        builder.ToTable("aik_routing_strategies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIRoutingStrategyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.TargetPersona).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetUseCase).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetClientType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PreferredPath).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Priority);
    }
}
