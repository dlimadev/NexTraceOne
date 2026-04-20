using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade EvaluationDataset.
/// Tabela: aik_evaluation_datasets.
/// </summary>
public sealed class EvaluationDatasetConfiguration : IEntityTypeConfiguration<EvaluationDataset>
{
    public void Configure(EntityTypeBuilder<EvaluationDataset> builder)
    {
        builder.ToTable("aik_evaluation_datasets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => EvaluationDatasetId.From(value));

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.UseCase).HasMaxLength(200).IsRequired();

        builder.Property(e => e.SourceType)
            .HasConversion(v => v.ToString(), v => Enum.Parse<EvaluationDatasetSourceType>(v))
            .HasMaxLength(50);

        builder.Property(e => e.CaseCount);
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(500);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_aik_eval_datasets_tenant");
    }
}
