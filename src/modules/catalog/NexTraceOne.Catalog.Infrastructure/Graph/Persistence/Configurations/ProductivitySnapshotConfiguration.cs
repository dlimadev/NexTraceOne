using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade ProductivitySnapshot.</summary>
internal sealed class ProductivitySnapshotConfiguration : IEntityTypeConfiguration<ProductivitySnapshot>
{
    public void Configure(EntityTypeBuilder<ProductivitySnapshot> builder)
    {
        builder.ToTable("cat_productivity_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ProductivitySnapshotId.From(value));

        builder.Property(x => x.TeamId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.DeploymentCount).IsRequired();
        builder.Property(x => x.AverageCycleTimeHours).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.IncidentCount).IsRequired();
        builder.Property(x => x.ManualStepsCount).IsRequired();
        builder.Property(x => x.SnapshotSource).HasMaxLength(100);
        builder.Property(x => x.RecordedAt).IsRequired();

        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.PeriodStart);
        builder.HasIndex(x => x.PeriodEnd);
    }
}
