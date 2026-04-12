using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade PipelineExecution.
/// Execuções de pipeline rastreiam a geração automatizada de código a partir de contratos.
/// </summary>
internal sealed class PipelineExecutionConfiguration : IEntityTypeConfiguration<PipelineExecution>
{
    public void Configure(EntityTypeBuilder<PipelineExecution> builder)
    {
        builder.ToTable("cat_pipeline_executions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PipelineExecutionId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ContractName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ContractVersion).IsRequired().HasMaxLength(100);

        builder.Property(x => x.RequestedStages)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.StageResults)
            .HasColumnType("jsonb");

        builder.Property(x => x.GeneratedArtifacts)
            .HasColumnType("jsonb");

        builder.Property(x => x.TargetLanguage).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetFramework).HasMaxLength(100);

        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.TotalStages).IsRequired();
        builder.Property(x => x.CompletedStages).IsRequired();
        builder.Property(x => x.FailedStages).IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DurationMs);

        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.InitiatedByUserId).IsRequired().HasMaxLength(200);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedAt);
    }
}
