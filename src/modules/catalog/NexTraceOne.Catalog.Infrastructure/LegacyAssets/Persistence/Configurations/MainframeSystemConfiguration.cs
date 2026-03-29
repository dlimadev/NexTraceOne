using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class MainframeSystemConfiguration : IEntityTypeConfiguration<MainframeSystem>
{
    public void Configure(EntityTypeBuilder<MainframeSystem> builder)
    {
        builder.ToTable("cat_mainframe_systems", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_mainframe_systems_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_mainframe_systems_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MainframeSystemId.From(value));

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── LPAR (Value Object) ───────────────────────────────────────
        builder.OwnsOne(x => x.Lpar, lpar =>
        {
            lpar.Property(l => l.SysplexName).HasMaxLength(100).HasColumnName("SysplexName");
            lpar.Property(l => l.LparName).HasMaxLength(100).HasColumnName("LparName");
            lpar.Property(l => l.RegionName).HasMaxLength(100).HasColumnName("RegionName");
        });

        // ── Ownership ─────────────────────────────────────────────────
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Domain).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TechnicalOwner).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.BusinessOwner).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Classificação ─────────────────────────────────────────────
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Criticality.Medium);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(50).HasDefaultValue(LifecycleStatus.Active);

        // ── Metadata ──────────────────────────────────────────────────
        builder.Property(x => x.OperatingSystem).HasMaxLength(100).HasDefaultValue(string.Empty);
        builder.Property(x => x.MipsCapacity).HasMaxLength(50).HasDefaultValue(string.Empty);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.TeamName);
        builder.HasIndex(x => x.Domain);
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.LifecycleStatus);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
