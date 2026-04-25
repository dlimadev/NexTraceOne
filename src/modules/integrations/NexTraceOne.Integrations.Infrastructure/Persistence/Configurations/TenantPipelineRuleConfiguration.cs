using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade TenantPipelineRule.
/// Tabela: int_tenant_pipeline_rules
/// </summary>
internal sealed class TenantPipelineRuleConfiguration : IEntityTypeConfiguration<TenantPipelineRule>
{
    public void Configure(EntityTypeBuilder<TenantPipelineRule> builder)
    {
        builder.ToTable("int_tenant_pipeline_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TenantPipelineRuleId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.RuleType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SignalType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ConditionJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ActionJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.SignalType, x.IsEnabled });
        builder.HasIndex(x => new { x.TenantId, x.RuleType, x.Priority });
    }
}
