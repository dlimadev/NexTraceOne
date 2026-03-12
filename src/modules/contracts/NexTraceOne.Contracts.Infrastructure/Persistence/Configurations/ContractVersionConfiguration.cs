using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Contracts.Domain.Entities;

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
