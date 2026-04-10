using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ImpactSimulation.
/// Simulações de impacto de dependências entre serviços para cenários what-if.
/// </summary>
internal sealed class ImpactSimulationConfiguration : IEntityTypeConfiguration<ImpactSimulation>
{
    public void Configure(EntityTypeBuilder<ImpactSimulation> builder)
    {
        builder.ToTable("cat_impact_simulations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ImpactSimulationId.From(value));

        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(200);

        builder.Property(x => x.Scenario)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ScenarioDescription).IsRequired().HasMaxLength(4000);

        builder.Property(x => x.AffectedServices).HasColumnType("jsonb");
        builder.Property(x => x.BrokenConsumers).HasColumnType("jsonb");

        builder.Property(x => x.TransitiveCascadeDepth).IsRequired();
        builder.Property(x => x.RiskPercent).IsRequired();

        builder.Property(x => x.MitigationRecommendations).HasColumnType("jsonb");

        builder.Property(x => x.SimulatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId).HasMaxLength(200);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Scenario);
        builder.HasIndex(x => x.SimulatedAt);
        builder.HasIndex(x => x.RiskPercent);
    }
}
