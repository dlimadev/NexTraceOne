using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Configurations;

internal sealed class LintResultConfiguration : IEntityTypeConfiguration<LintResult>
{
    /// <summary>Configura o mapeamento da entidade LintResult para a tabela rg_lint_results.</summary>
    public void Configure(EntityTypeBuilder<LintResult> builder)
    {
        builder.ToTable("chg_lint_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LintResultId.From(value));

        builder.Property(x => x.RulesetId)
            .HasConversion(id => id.Value, value => RulesetId.From(value))
            .IsRequired();
        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.Score)
            .HasColumnType("numeric(5,2)")
            .HasPrecision(5, 2)
            .IsRequired();
        builder.Property(x => x.TotalFindings).IsRequired();

        builder.Property(x => x.Findings)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<Finding>)System.Text.Json.JsonSerializer.Deserialize<List<Finding>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.ExecutedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}
