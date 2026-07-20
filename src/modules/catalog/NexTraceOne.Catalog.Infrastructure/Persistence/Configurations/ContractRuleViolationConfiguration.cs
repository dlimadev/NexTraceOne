using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core de <see cref="ContractRuleViolation"/>.
/// Mapeia a FK typed-id <c>ContractVersionId</c> à coluna existente
/// (<c>HasColumnName</c>) — sem esta config a convenção não a mapeava e o EF caía
/// numa coluna shadow <c>ContractVersionId1</c>. Preserva os dados (não move coluna).
/// </summary>
internal sealed class ContractRuleViolationConfiguration : IEntityTypeConfiguration<ContractRuleViolation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContractRuleViolation> builder)
    {
        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value))
            .HasColumnName("ContractVersionId1");
    }
}
