using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Infrastructure.Persistence.Configurations;

internal sealed class AIRoutingDecisionConfiguration : IEntityTypeConfiguration<AIRoutingDecision>
{
    public void Configure(EntityTypeBuilder<AIRoutingDecision> builder)
    {
        builder.ToTable("ai_gov_routing_decisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIRoutingDecisionId.From(value));

        builder.Property(x => x.CorrelationId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UseCaseType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.ClientType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SelectedPath).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.SelectedModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SelectedProvider).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AppliedPolicyName).HasMaxLength(200);
        builder.Property(x => x.EscalationReason).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Rationale).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EstimatedCostClass).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConfidenceLevel).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.SelectedSources).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SourceWeightingSummary).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.DecidedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.DecidedAt);
        builder.HasIndex(x => x.SelectedPath);
    }
}
