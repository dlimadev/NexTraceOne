using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

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
