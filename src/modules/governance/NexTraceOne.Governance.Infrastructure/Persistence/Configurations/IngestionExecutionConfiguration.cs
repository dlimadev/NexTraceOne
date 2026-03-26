using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade IngestionExecution.
/// Define mapeamento de tabela, typed ID, enums, relacionamentos e índices.
/// </summary>
internal sealed class IngestionExecutionConfiguration : IEntityTypeConfiguration<IngestionExecution>
{
    public void Configure(EntityTypeBuilder<IngestionExecution> builder)
    {
        builder.ToTable("int_ingestion_executions", t =>
        {
            t.HasCheckConstraint("CK_int_ingestion_executions_result",
                "\"Result\" IN ('Running','Success','PartialSuccess','Failed','Cancelled','TimedOut')");
        });

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

        builder.Property(x => x.RetryAttempt)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relacionamentos
        // NOTE P2.1: FK navigation to IntegrationConnector removed because IntegrationConnector
        // moved to IntegrationsDbContext. ConnectorId is preserved as a cross-context reference.
        // EF navigation will be restored when IngestionExecution is extracted in P2.2.

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
        builder.HasIndex(x => x.RetryAttempt);
        builder.HasIndex(x => new { x.ConnectorId, x.StartedAt });
    }
}
