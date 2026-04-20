using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade EvaluationCase.
/// Tabela: aik_evaluation_cases.
/// </summary>
public sealed class EvaluationCaseConfiguration : IEntityTypeConfiguration<EvaluationCase>
{
    public void Configure(EntityTypeBuilder<EvaluationCase> builder)
    {
        builder.ToTable("aik_evaluation_cases");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => EvaluationCaseId.From(value));

        builder.Property(e => e.SuiteId)
            .HasConversion(id => id.Value, value => EvaluationSuiteId.From(value))
            .IsRequired();

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.InputPrompt).HasColumnType("text").IsRequired();
        builder.Property(e => e.GroundingContext).HasColumnType("text");
        builder.Property(e => e.ExpectedOutputPattern).HasColumnType("text");
        builder.Property(e => e.EvaluationCriteria).HasMaxLength(500);
        builder.Property(e => e.IsActive).IsRequired();

        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(500);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(500);

        builder.HasIndex(e => e.SuiteId).HasDatabaseName("idx_aik_eval_cases_suite");
    }
}
