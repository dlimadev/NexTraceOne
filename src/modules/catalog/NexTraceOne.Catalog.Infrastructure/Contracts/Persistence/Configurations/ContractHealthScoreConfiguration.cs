using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractHealthScore.
/// Scores de saúde avaliam a qualidade contínua de contratos (API Assets).
/// </summary>
internal sealed class ContractHealthScoreConfiguration : IEntityTypeConfiguration<ContractHealthScore>
{
    public void Configure(EntityTypeBuilder<ContractHealthScore> builder)
    {
        builder.ToTable("cat_contract_health_scores");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractHealthScoreId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();

        builder.Property(x => x.OverallScore).IsRequired();
        builder.Property(x => x.BreakingChangeFrequencyScore).IsRequired();
        builder.Property(x => x.ConsumerImpactScore).IsRequired();
        builder.Property(x => x.ReviewRecencyScore).IsRequired();
        builder.Property(x => x.ExampleCoverageScore).IsRequired();
        builder.Property(x => x.PolicyComplianceScore).IsRequired();
        builder.Property(x => x.DocumentationScore).IsRequired();

        builder.Property(x => x.CalculatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.IsDegraded).IsRequired();
        builder.Property(x => x.DegradationThreshold).IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ApiAssetId).IsUnique();
        builder.HasIndex(x => x.OverallScore);
        builder.HasIndex(x => x.IsDegraded);
    }
}
