using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractDeployment.
/// Regista deployments de versões de contrato por ambiente para Change Intelligence.
/// </summary>
internal sealed class ContractDeploymentConfiguration : IEntityTypeConfiguration<ContractDeployment>
{
    /// <summary>Configura o mapeamento da entidade ContractDeployment para a tabela ctr_contract_deployments.</summary>
    public void Configure(EntityTypeBuilder<ContractDeployment> builder)
    {
        builder.ToTable("ctr_contract_deployments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractDeploymentId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ApiAssetId).IsRequired();

        builder.Property(x => x.Environment)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SemVer)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContractDeploymentStatus.Pending)
            .IsRequired();

        builder.Property(x => x.DeployedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.DeployedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => new { x.ContractVersionId, x.Environment });
        builder.HasIndex(x => x.DeployedAt);
    }
}
