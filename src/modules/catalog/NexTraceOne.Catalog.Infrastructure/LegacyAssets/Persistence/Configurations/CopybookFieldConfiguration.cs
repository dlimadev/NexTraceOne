using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookFieldConfiguration : IEntityTypeConfiguration<CopybookField>
{
    public void Configure(EntityTypeBuilder<CopybookField> builder)
    {
        builder.ToTable("cat_copybook_fields");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookFieldId.From(value));

        // ── FK para Copybook ──────────────────────────────────────────
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => CopybookId.From(value));
        builder.HasOne<Copybook>()
            .WithMany()
            .HasForeignKey(x => x.CopybookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Estrutura do campo ────────────────────────────────────────
        builder.Property(x => x.FieldName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PicClause).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.DataType).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(x => x.RedefinesField).HasMaxLength(200);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.CopybookId);
        builder.HasIndex(x => new { x.CopybookId, x.SortOrder });

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
