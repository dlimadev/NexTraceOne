using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade ReleaseBaseline.</summary>
internal sealed class ReleaseBaselineConfiguration : IEntityTypeConfiguration<ReleaseBaseline>
{
    /// <summary>Configura o mapeamento da entidade ReleaseBaseline para a tabela ci_release_baselines.</summary>
    public void Configure(EntityTypeBuilder<ReleaseBaseline> builder)
    {
        builder.ToTable("ci_release_baselines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseBaselineId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.RequestsPerMinute).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ErrorRate).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.AvgLatencyMs).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.P95LatencyMs).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.P99LatencyMs).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Throughput).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CollectedFrom).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CollectedTo).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId).IsUnique();
    }
}
