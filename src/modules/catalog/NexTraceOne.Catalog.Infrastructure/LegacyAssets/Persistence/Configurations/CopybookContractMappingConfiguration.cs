using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookContractMappingConfiguration : IEntityTypeConfiguration<CopybookContractMapping>
{
    public void Configure(EntityTypeBuilder<CopybookContractMapping> builder)
    {
        builder.ToTable("cat_copybook_contract_mappings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookContractMappingId.From(value));

        // ── FK para Copybook ──────────────────────────────────────────
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => CopybookId.From(value));
        builder.HasOne<Copybook>()
            .WithMany()
            .HasForeignKey(x => x.CopybookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Propriedades ──────────────────────────────────────────────
        builder.Property(x => x.MappingType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CopybookId, x.ContractVersionId }).IsUnique();
        builder.HasIndex(x => x.CopybookId);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
