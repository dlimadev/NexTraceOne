using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AiEvalDataset. Table: aik_eval_datasets.
/// CC-05: AI Evaluation Harness — model comparison datasets.
/// </summary>
public sealed class AiEvalDatasetConfiguration : IEntityTypeConfiguration<AiEvalDataset>
{
    public void Configure(EntityTypeBuilder<AiEvalDataset> builder)
    {
        builder.ToTable("aik_eval_datasets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, v => new AiEvalDatasetId(v));

        builder.Property(e => e.TenantId).HasColumnName("tenant_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.UseCase).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.TestCasesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.TestCaseCount).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(e => new { e.TenantId, e.UseCase })
            .HasDatabaseName("ix_aik_eval_datasets_tenant_usecase");
    }
}
