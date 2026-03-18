using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade IngestionExecution.
/// Define mapeamento de tabela, typed ID, enums, relacionamentos e índices.
/// </summary>
internal sealed class IngestionExecutionConfiguration : IEntityTypeConfiguration<IngestionExecution>
{
    public void Configure(EntityTypeBuilder<IngestionExecution> builder)
    {
        builder.ToTable("gov_ingestion_executions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IngestionExecutionId(value));

        builder.Property(x => x.ConnectorId)
            .HasConversion(id => id.Value, value => new IntegrationConnectorId(value))
            .IsRequired();

        builder.Property(x => x.SourceId)
            .HasConversion(id => id!.Value, value => value != Guid.Empty ? new IngestionSourceId(value) : null);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.StartedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DurationMs);

        builder.Property(x => x.Result)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ItemsProcessed)
            .IsRequired();

        builder.Property(x => x.ItemsSucceeded)
            .IsRequired();

        builder.Property(x => x.ItemsFailed)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relacionamentos
        builder.HasOne<IntegrationConnector>()
            .WithMany()
            .HasForeignKey(x => x.ConnectorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<IngestionSource>()
            .WithMany()
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices para consultas frequentes
        builder.HasIndex(x => x.ConnectorId);
        builder.HasIndex(x => x.SourceId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.StartedAt);
        builder.HasIndex(x => x.Result);
        builder.HasIndex(x => new { x.ConnectorId, x.StartedAt });
    }
}
