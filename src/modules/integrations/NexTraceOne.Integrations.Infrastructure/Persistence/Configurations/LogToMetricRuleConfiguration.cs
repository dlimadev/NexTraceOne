using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade LogToMetricRule.
/// Tabela: int_log_to_metric_rules
/// </summary>
internal sealed class LogToMetricRuleConfiguration : IEntityTypeConfiguration<LogToMetricRule>
{
    public void Configure(EntityTypeBuilder<LogToMetricRule> builder)
    {
        builder.ToTable("int_log_to_metric_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new LogToMetricRuleId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Pattern)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.MetricName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MetricType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ValueExtractor)
            .HasMaxLength(500);

        builder.Property(x => x.LabelsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsEnabled });
    }
}
