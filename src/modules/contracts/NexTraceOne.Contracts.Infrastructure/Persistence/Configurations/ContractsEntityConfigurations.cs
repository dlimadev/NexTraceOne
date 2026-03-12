using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Configurations;

internal sealed class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    /// <summary>Configura o mapeamento da entidade ContractVersion para a tabela ct_contract_versions.</summary>
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.ToTable("ct_contract_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractVersionId.From(value));
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.SemVer).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SpecContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ImportedFrom).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.LockedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LockedBy).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.ApiAssetId, x.SemVer }).IsUnique();

        builder.HasMany(x => x.Diffs)
            .WithOne()
            .HasForeignKey("ContractVersionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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
