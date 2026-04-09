using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade SchemaEvolutionAdvice.
/// Análises de evolução de schema avaliam compatibilidade entre versões de contratos.
/// </summary>
internal sealed class SchemaEvolutionAdviceConfiguration : IEntityTypeConfiguration<SchemaEvolutionAdvice>
{
    public void Configure(EntityTypeBuilder<SchemaEvolutionAdvice> builder)
    {
        builder.ToTable("cat_schema_evolution_advices");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SchemaEvolutionAdviceId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ContractName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SourceVersion).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetVersion).IsRequired().HasMaxLength(50);

        builder.Property(x => x.CompatibilityLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CompatibilityScore).IsRequired();

        builder.Property(x => x.FieldsAdded).HasColumnType("jsonb");
        builder.Property(x => x.FieldsRemoved).HasColumnType("jsonb");
        builder.Property(x => x.FieldsModified).HasColumnType("jsonb");
        builder.Property(x => x.FieldsInUseByConsumers).HasColumnType("jsonb");
        builder.Property(x => x.AffectedConsumers).HasColumnType("jsonb");

        builder.Property(x => x.AffectedConsumerCount).IsRequired();

        builder.Property(x => x.RecommendedStrategy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.StrategyDetails).HasColumnType("jsonb");
        builder.Property(x => x.Recommendations).HasColumnType("jsonb");
        builder.Property(x => x.Warnings).HasColumnType("jsonb");

        builder.Property(x => x.AnalyzedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.AnalyzedByAgentName).HasMaxLength(200);

        builder.Property(x => x.TenantId);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.AnalyzedAt);
        builder.HasIndex(x => x.CompatibilityLevel);
    }
}
