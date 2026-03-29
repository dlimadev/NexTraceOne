using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class CopybookConfiguration : IEntityTypeConfiguration<Copybook>
{
    public void Configure(EntityTypeBuilder<Copybook> builder)
    {
        builder.ToTable("cat_copybooks", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_copybooks_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_copybooks_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CopybookId.From(value));

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── FK para MainframeSystem ───────────────────────────────────
        builder.Property(x => x.SystemId)
            .HasConversion(id => id.Value, value => MainframeSystemId.From(value));
        builder.HasOne<MainframeSystem>()
            .WithMany()
            .HasForeignKey(x => x.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Estrutura ─────────────────────────────────────────────────
        builder.Property(x => x.Version).HasMaxLength(50).HasDefaultValue(string.Empty);
        builder.Property(x => x.SourceLibrary).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.RawContent).HasDefaultValue(string.Empty);

        // ── Layout (Value Object) ─────────────────────────────────────
        builder.OwnsOne(x => x.Layout, layout =>
        {
            layout.Property(l => l.FieldCount).HasColumnName("LayoutFieldCount");
            layout.Property(l => l.TotalLength).HasColumnName("LayoutTotalLength");
            layout.Property(l => l.RecordFormat).HasMaxLength(20).HasColumnName("LayoutRecordFormat");
        });

        // ── Classificação ─────────────────────────────────────────────
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Criticality.Medium);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(50).HasDefaultValue(LifecycleStatus.Active);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.Name, x.SystemId }).IsUnique();
        builder.HasIndex(x => x.SystemId);
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.LifecycleStatus);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
