using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookDiffRecordConfiguration : IEntityTypeConfiguration<CopybookDiffRecord>
{
    public void Configure(EntityTypeBuilder<CopybookDiffRecord> builder)
    {
        builder.ToTable("cat_copybook_diffs", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_copybook_diffs_change_level",
                "\"ChangeLevel\" >= 0 AND \"ChangeLevel\" <= 4");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookDiffRecordId.From(value));

        // ── FK para Copybook ──────────────────────────────────────────
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => CopybookId.From(value));
        builder.HasOne<Copybook>()
            .WithMany()
            .HasForeignKey(x => x.CopybookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FKs para CopybookVersion ──────────────────────────────────
        builder.Property(x => x.BaseVersionId)
            .HasConversion(id => id.Value, value => CopybookVersionId.From(value));
        builder.HasOne<CopybookVersion>()
            .WithMany()
            .HasForeignKey(x => x.BaseVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.TargetVersionId)
            .HasConversion(id => id.Value, value => CopybookVersionId.From(value));
        builder.HasOne<CopybookVersion>()
            .WithMany()
            .HasForeignKey(x => x.TargetVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Propriedades ──────────────────────────────────────────────
        builder.Property(x => x.ChangeLevel).IsRequired();
        builder.Property(x => x.ChangesJson).HasColumnType("jsonb").IsRequired();

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.CopybookId);
        builder.HasIndex(x => new { x.BaseVersionId, x.TargetVersionId });

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
