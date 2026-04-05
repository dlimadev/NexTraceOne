using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade DxScore.</summary>
internal sealed class DxScoreConfiguration : IEntityTypeConfiguration<DxScore>
{
    public void Configure(EntityTypeBuilder<DxScore> builder)
    {
        builder.ToTable("cat_dx_scores");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DxScoreId.From(value));

        builder.Property(x => x.TeamId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CycleTimeHours).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.DeploymentFrequencyPerWeek).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.CognitivLoadScore).HasPrecision(6, 4).IsRequired();
        builder.Property(x => x.ToilPercentage).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.OverallScore).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.ScoreLevel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ComputedAt).IsRequired();

        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.Period);
        builder.HasIndex(x => x.ScoreLevel);
    }
}
