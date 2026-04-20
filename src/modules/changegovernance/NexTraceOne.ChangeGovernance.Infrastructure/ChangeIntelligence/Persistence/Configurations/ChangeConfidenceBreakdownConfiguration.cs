using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class ChangeConfidenceBreakdownConfiguration : IEntityTypeConfiguration<ChangeConfidenceBreakdown>
{
    /// <summary>Configura o mapeamento da entidade ChangeConfidenceBreakdown para a tabela chg_confidence_breakdowns.</summary>
    public void Configure(EntityTypeBuilder<ChangeConfidenceBreakdown> builder)
    {
        builder.ToTable("chg_confidence_breakdowns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChangeConfidenceBreakdownId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();

        builder.Property(x => x.AggregatedScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(x => x.ComputedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ScoreVersion)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();

        // ── Sub-scores como owned type em tabela separada ────────────────────
        builder.OwnsMany(x => x.SubScores, subScoreBuilder =>
        {
            subScoreBuilder.ToTable("chg_confidence_sub_scores");
            subScoreBuilder.WithOwner().HasForeignKey("BreakdownId");
            subScoreBuilder.Property<int>("Id").ValueGeneratedOnAdd();
            subScoreBuilder.HasKey("Id");

            subScoreBuilder.Property(s => s.SubScoreType)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            subScoreBuilder.Property(s => s.Value)
                .HasPrecision(7, 2)
                .IsRequired();

            subScoreBuilder.Property(s => s.Weight)
                .HasPrecision(7, 4)
                .IsRequired();

            subScoreBuilder.Property(s => s.Confidence)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            subScoreBuilder.Property(s => s.Reason)
                .HasMaxLength(2000)
                .IsRequired();

            subScoreBuilder.Property(s => s.Citations)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("jsonb")
                .IsRequired();

            subScoreBuilder.Property(s => s.SimulatedNote)
                .HasMaxLength(500);
        });
    }
}
