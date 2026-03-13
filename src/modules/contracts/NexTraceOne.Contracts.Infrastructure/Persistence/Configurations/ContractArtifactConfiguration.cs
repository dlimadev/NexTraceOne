using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractArtifact.
/// Artefatos gerados (testes, scaffolds, evidências) são entidades filhas de ContractVersion.
/// </summary>
internal sealed class ContractArtifactConfiguration : IEntityTypeConfiguration<ContractArtifact>
{
    /// <summary>Configura o mapeamento para a tabela ct_contract_artifacts.</summary>
    public void Configure(EntityTypeBuilder<ContractArtifact> builder)
    {
        builder.ToTable("ct_contract_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractArtifactId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ArtifactType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.ContentFormat).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsAiGenerated).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.GeneratedBy).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.ArtifactType);
    }
}
