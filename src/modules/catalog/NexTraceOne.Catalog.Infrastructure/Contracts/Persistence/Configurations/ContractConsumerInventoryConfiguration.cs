using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para ContractConsumerInventory.
/// Tabela: ctr_contract_consumer_inventory
/// </summary>
internal sealed class ContractConsumerInventoryConfiguration : IEntityTypeConfiguration<ContractConsumerInventory>
{
    public void Configure(EntityTypeBuilder<ContractConsumerInventory> builder)
    {
        builder.ToTable("ctr_contract_consumer_inventory");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractConsumerInventoryId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ContractId).IsRequired();

        builder.Property(x => x.ConsumerService)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConsumerEnvironment)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasMaxLength(50);

        builder.Property(x => x.FrequencyPerDay).IsRequired();

        builder.Property(x => x.LastCalledAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.FirstCalledAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ContractId })
            .HasDatabaseName("ix_ctr_consumer_inventory_tenant_contract");

        builder.HasIndex(x => new { x.TenantId, x.ContractId, x.ConsumerService, x.ConsumerEnvironment })
            .IsUnique()
            .HasDatabaseName("ix_ctr_consumer_inventory_unique_consumer");
    }
}
