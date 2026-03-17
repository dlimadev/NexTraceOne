using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractDiff (multi-protocolo).
/// Inclui coluna de protocolo e nível de confiança da sugestão de semver.
/// </summary>
internal sealed class ContractDiffConfiguration : IEntityTypeConfiguration<ContractDiff>
{
    /// <summary>Configura o mapeamento da entidade ContractDiff para a tabela ct_contract_diffs.</summary>
    public void Configure(EntityTypeBuilder<ContractDiff> builder)
    {
        builder.ToTable("ct_contract_diffs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractDiffId.From(value));

        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .HasColumnName("ContractVersionId")
            .IsRequired();

        builder.Property(x => x.BaseVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.TargetVersionId)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value))
            .IsRequired();

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ChangeLevel).IsRequired();

        builder.Property(x => x.Protocol)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContractProtocol.OpenApi)
            .IsRequired();

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.8m);

        builder.Property(x => x.BreakingChanges)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<ChangeEntry>)System.Text.Json.JsonSerializer.Deserialize<List<ChangeEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.NonBreakingChanges)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<ChangeEntry>)System.Text.Json.JsonSerializer.Deserialize<List<ChangeEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.AdditiveChanges)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<ChangeEntry>)System.Text.Json.JsonSerializer.Deserialize<List<ChangeEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("text");

        builder.Property(x => x.SuggestedSemVer).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone").IsRequired();
    }
}
