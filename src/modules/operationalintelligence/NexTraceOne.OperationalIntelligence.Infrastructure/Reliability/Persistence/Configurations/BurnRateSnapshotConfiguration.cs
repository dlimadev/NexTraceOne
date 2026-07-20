using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>
/// Configuração EF Core de <see cref="BurnRateSnapshot"/>.
/// Mapeia a FK typed-id <c>SloDefinitionId</c> à coluna existente
/// (<c>HasColumnName</c>) — sem esta config a convenção não a mapeava e o EF caía
/// numa coluna shadow <c>SloDefinitionId1</c>. Preserva os dados (não move coluna).
/// </summary>
internal sealed class BurnRateSnapshotConfiguration : IEntityTypeConfiguration<BurnRateSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BurnRateSnapshot> builder)
    {
        builder.Property(x => x.SloDefinitionId)
            .HasConversion(id => id.Value, value => SloDefinitionId.From(value))
            .HasColumnName("SloDefinitionId1");
    }
}
