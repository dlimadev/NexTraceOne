using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade GovernanceDomain.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// </summary>
internal sealed class GovernanceDomainConfiguration : IEntityTypeConfiguration<GovernanceDomain>
{
    public void Configure(EntityTypeBuilder<GovernanceDomain> builder)
    {
        builder.ToTable("gov_domains");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GovernanceDomainId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Criticality)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CapabilityClassification)
            .HasMaxLength(200);

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.CapabilityClassification);
    }
}
