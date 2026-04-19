using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiAgentPerformanceMetric.
/// Tabela: aik_agent_performance_metrics.
/// </summary>
public sealed class AiAgentPerformanceMetricConfiguration : IEntityTypeConfiguration<AiAgentPerformanceMetric>
{
    public void Configure(EntityTypeBuilder<AiAgentPerformanceMetric> builder)
    {
        builder.ToTable("aik_agent_performance_metrics");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => AiAgentPerformanceMetricId.From(value));

        builder.Property(e => e.AgentId)
            .HasConversion(
                id => id.Value,
                value => AiAgentId.From(value))
            .IsRequired();

        builder.Property(e => e.AgentName).HasMaxLength(300).IsRequired();

        builder.HasIndex(e => e.AgentId);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.PeriodStart);
    }
}
