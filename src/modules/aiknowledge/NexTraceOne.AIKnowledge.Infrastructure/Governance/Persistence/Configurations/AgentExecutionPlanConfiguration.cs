using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

using System.Text.Json;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AgentExecutionPlanConfiguration : IEntityTypeConfiguration<AgentExecutionPlan>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AgentExecutionPlan> builder)
    {
        builder.ToTable("aik_agent_execution_plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AgentExecutionPlanId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.PlanStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.MaxTokenBudget).IsRequired();
        builder.Property(x => x.ConsumedTokens).IsRequired();
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.BlastRadiusThreshold).IsRequired();
        builder.Property(x => x.ApprovedBy).HasMaxLength(500);
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);

        // AgentStep is a value object — stored as JSONB
        builder.Property<List<AgentStep>>("_steps")
            .HasField("_steps")
            .HasColumnName("steps")
            .HasColumnType("jsonb")
            .HasConversion(
                steps => JsonSerializer.Serialize(steps, JsonOptions),
                json => JsonSerializer.Deserialize<List<AgentStep>>(json, JsonOptions) ?? new List<AgentStep>())
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.PlanStatus);
    }
}
