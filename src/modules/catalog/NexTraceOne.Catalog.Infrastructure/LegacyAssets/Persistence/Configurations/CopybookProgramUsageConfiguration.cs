using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookProgramUsageConfiguration : IEntityTypeConfiguration<CopybookProgramUsage>
{
    public void Configure(EntityTypeBuilder<CopybookProgramUsage> builder)
    {
        builder.ToTable("cat_copybook_program_usages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookProgramUsageId.From(value));

        // ── FK para CobolProgram ──────────────────────────────────────
        builder.Property(x => x.ProgramId)
            .HasConversion(id => id.Value, value => CobolProgramId.From(value));
        builder.HasOne<CobolProgram>()
            .WithMany()
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK para Copybook ──────────────────────────────────────────
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => CopybookId.From(value));
        builder.HasOne<Copybook>()
            .WithMany()
            .HasForeignKey(x => x.CopybookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Propriedades ──────────────────────────────────────────────
        builder.Property(x => x.UsageType).HasMaxLength(50).HasDefaultValue("COPY");

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.ProgramId, x.CopybookId }).IsUnique();
        builder.HasIndex(x => x.CopybookId);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
