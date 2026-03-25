using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade ObservationWindow.</summary>
internal sealed class ObservationWindowConfiguration : IEntityTypeConfiguration<ObservationWindow>
{
    /// <summary>Configura o mapeamento da entidade ObservationWindow para a tabela ci_observation_windows.</summary>
    public void Configure(EntityTypeBuilder<ObservationWindow> builder)
    {
        builder.ToTable("chg_observation_windows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ObservationWindowId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.Phase).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.StartsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RequestsPerMinute).HasPrecision(18, 4);
        builder.Property(x => x.ErrorRate).HasPrecision(18, 6);
        builder.Property(x => x.AvgLatencyMs).HasPrecision(18, 4);
        builder.Property(x => x.P95LatencyMs).HasPrecision(18, 4);
        builder.Property(x => x.P99LatencyMs).HasPrecision(18, 4);
        builder.Property(x => x.Throughput).HasPrecision(18, 4);
        builder.Property(x => x.IsCollected).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CollectedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.ReleaseId, x.Phase }).IsUnique();
    }
}
