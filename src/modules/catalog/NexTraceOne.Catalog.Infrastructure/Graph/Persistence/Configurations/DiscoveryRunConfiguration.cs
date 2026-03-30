using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a tabela cat_discovery_runs.
/// Regista execuções do job de discovery automático para auditoria.
/// </summary>
internal sealed class DiscoveryRunConfiguration : IEntityTypeConfiguration<DiscoveryRun>
{
    public void Configure(EntityTypeBuilder<DiscoveryRun> builder)
    {
        builder.ToTable("cat_discovery_runs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DiscoveryRunId.From(value));

        builder.Property(x => x.Source).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.ServicesFound).IsRequired();
        builder.Property(x => x.NewServicesFound).IsRequired();
        builder.Property(x => x.ErrorCount).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.StartedAt).IsDescending();
        builder.HasIndex(x => x.Environment);
    }
}
