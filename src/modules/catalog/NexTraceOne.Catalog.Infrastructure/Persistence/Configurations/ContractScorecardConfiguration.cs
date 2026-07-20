using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core de <see cref="ContractScorecard"/>.
/// Mapeia explicitamente a FK typed-id <c>ContractVersionId</c> — sem esta
/// configuração a convenção não a mapeia (só mapeia a chave primária), pelo que
/// consultas que filtram por versão do contrato ficavam sem tradução.
/// </summary>
internal sealed class ContractScorecardConfiguration : IEntityTypeConfiguration<ContractScorecard>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContractScorecard> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractScorecardId(value));
        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.HasIndex(x => x.ContractVersionId);
    }
}
