using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    /// <summary>Configura o mapeamento da entidade Release para a tabela ci_releases.</summary>
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("ci_releases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PipelineSource).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CommitSha).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeLevel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ChangeScore)
            .HasColumnType("numeric(5,4)")
            .HasPrecision(5, 4)
            .IsRequired()
            .HasDefaultValue(0.0m);
        builder.Property(x => x.WorkItemReference).HasMaxLength(500);
        builder.Property(x => x.RolledBackFromReleaseId)
            .HasConversion(
                id => id != null ? (Guid?)id.Value : null,
                value => value.HasValue ? ReleaseId.From(value.Value) : null);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ApiAssetId);
    }
}

internal sealed class BlastRadiusReportConfiguration : IEntityTypeConfiguration<BlastRadiusReport>
{
    /// <summary>Configura o mapeamento da entidade BlastRadiusReport para a tabela ci_blast_radius_reports.</summary>
    public void Configure(EntityTypeBuilder<BlastRadiusReport> builder)
    {
        builder.ToTable("ci_blast_radius_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BlastRadiusReportId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.TotalAffectedConsumers).IsRequired();

        builder.Property(x => x.DirectConsumers)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.TransitiveConsumers)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.CalculatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}

internal sealed class ChangeIntelligenceScoreConfiguration : IEntityTypeConfiguration<ChangeIntelligenceScore>
{
    /// <summary>Configura o mapeamento da entidade ChangeIntelligenceScore para a tabela ci_change_scores.</summary>
    public void Configure(EntityTypeBuilder<ChangeIntelligenceScore> builder)
    {
        builder.ToTable("ci_change_scores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChangeIntelligenceScoreId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.Score).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.BreakingChangeWeight).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.BlastRadiusWeight).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.EnvironmentWeight).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}

internal sealed class ChangeEventConfiguration : IEntityTypeConfiguration<ChangeEvent>
{
    /// <summary>Configura o mapeamento da entidade ChangeEvent para a tabela ci_change_events.</summary>
    public void Configure(EntityTypeBuilder<ChangeEvent> builder)
    {
        builder.ToTable("ci_change_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChangeEventId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Source).HasMaxLength(500).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}
