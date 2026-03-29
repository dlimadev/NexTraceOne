using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookVersionConfiguration : IEntityTypeConfiguration<CopybookVersion>
{
    public void Configure(EntityTypeBuilder<CopybookVersion> builder)
    {
        builder.ToTable("cat_copybook_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookVersionId.From(value));

        // ── FK para Copybook ──────────────────────────────────────────
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => CopybookId.From(value));
        builder.HasOne<Copybook>()
            .WithMany()
            .HasForeignKey(x => x.CopybookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Propriedades ──────────────────────────────────────────────
        builder.Property(x => x.VersionLabel).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RawContent).IsRequired();
        builder.Property(x => x.RecordFormat).HasMaxLength(20).HasDefaultValue(string.Empty);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CopybookId, x.VersionLabel }).IsUnique();
        builder.HasIndex(x => x.CopybookId);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
