using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade BackgroundServiceContractDetail.
/// Armazena metadados específicos de Background Service Contracts publicados.
/// Tabela: ctr_background_service_contract_details, com FK para ctr_contract_versions.
/// </summary>
internal sealed class BackgroundServiceContractDetailConfiguration : IEntityTypeConfiguration<BackgroundServiceContractDetail>
{
    /// <summary>Configura o mapeamento da entidade BackgroundServiceContractDetail.</summary>
    public void Configure(EntityTypeBuilder<BackgroundServiceContractDetail> builder)
    {
        builder.ToTable("ctr_background_service_contract_details", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_bg_service_details_trigger_type",
                "\"TriggerType\" IN ('Cron', 'Interval', 'EventTriggered', 'OnDemand', 'Continuous')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BackgroundServiceContractDetailId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TriggerType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScheduleExpression).HasMaxLength(200);
        builder.Property(x => x.TimeoutExpression).HasMaxLength(50);
        builder.Property(x => x.AllowsConcurrency).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.InputsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.OutputsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.SideEffectsJson).HasColumnType("text").IsRequired();

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        // Um BackgroundServiceContractDetail por ContractVersion (1:0..1)
        builder.HasIndex(x => x.ContractVersionId).IsUnique();
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");
    }
}
