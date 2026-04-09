using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade SemanticDiffResult.
/// Resultados de diff semântico assistido por IA entre versões de contrato.
/// </summary>
internal sealed class SemanticDiffResultConfiguration : IEntityTypeConfiguration<SemanticDiffResult>
{
    public void Configure(EntityTypeBuilder<SemanticDiffResult> builder)
    {
        builder.ToTable("cat_semantic_diff_results");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SemanticDiffResultId.From(value));

        builder.Property(x => x.ContractVersionFromId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContractVersionToId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.NaturalLanguageSummary).IsRequired().HasMaxLength(8000);

        builder.Property(x => x.Classification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.AffectedConsumers).HasColumnType("jsonb");
        builder.Property(x => x.MitigationSuggestions).HasColumnType("jsonb");

        builder.Property(x => x.CompatibilityScore).IsRequired();

        builder.Property(x => x.GeneratedByModel).IsRequired().HasMaxLength(200);

        builder.Property(x => x.GeneratedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId).HasMaxLength(200);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ContractVersionFromId);
        builder.HasIndex(x => x.ContractVersionToId);
        builder.HasIndex(x => x.Classification);
        builder.HasIndex(x => x.GeneratedAt);
    }
}
