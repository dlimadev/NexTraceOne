using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade EvaluationSuite.
/// Tabela: aik_evaluation_suites.
/// </summary>
public sealed class EvaluationSuiteConfiguration : IEntityTypeConfiguration<EvaluationSuite>
{
    public void Configure(EntityTypeBuilder<EvaluationSuite> builder)
    {
        builder.ToTable("aik_evaluation_suites");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => EvaluationSuiteId.From(value));

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.UseCase).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Version).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.TargetModelId);

        builder.Property(e => e.Status)
            .HasConversion(v => v.ToString(), v => Enum.Parse<EvaluationSuiteStatus>(v))
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(500);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_aik_eval_suites_tenant");
        builder.HasIndex(e => new { e.TenantId, e.UseCase }).HasDatabaseName("idx_aik_eval_suites_tenant_usecase");
    }
}
