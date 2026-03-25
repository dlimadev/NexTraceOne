using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractScorecard.
/// Scorecards avaliam a qualidade técnica de versões de contrato.
/// </summary>
internal sealed class ContractScorecardConfiguration : IEntityTypeConfiguration<ContractScorecard>
{
    public void Configure(EntityTypeBuilder<ContractScorecard> builder)
    {
        builder.ToTable("ctr_contract_scorecards");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractScorecardId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.QualityScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.CompletenessScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.CompatibilityScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.RiskScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.OverallScore).HasPrecision(5, 4).IsRequired();

        builder.Property(x => x.OperationCount).IsRequired();
        builder.Property(x => x.SchemaCount).IsRequired();
        builder.Property(x => x.HasSecurityDefinitions).IsRequired();
        builder.Property(x => x.HasExamples).IsRequired();
        builder.Property(x => x.HasDescriptions).IsRequired();

        builder.Property(x => x.QualityJustification).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CompletenessJustification).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CompatibilityJustification).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RiskJustification).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.ComputedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // FK: ContractScorecard → ContractVersion
        builder.HasOne<ContractVersion>()
            .WithMany()
            .HasForeignKey(x => x.ContractVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.OverallScore);
    }
}
