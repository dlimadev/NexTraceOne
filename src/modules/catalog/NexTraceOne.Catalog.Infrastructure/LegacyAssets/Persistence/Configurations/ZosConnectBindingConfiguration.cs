using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class ZosConnectBindingConfiguration : IEntityTypeConfiguration<ZosConnectBinding>
{
    public void Configure(EntityTypeBuilder<ZosConnectBinding> builder)
    {
        builder.ToTable("cat_zos_connect_bindings", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_zos_connect_bindings_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_zos_connect_bindings_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ZosConnectBindingId.From(value));

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

        // ── Mapeamento REST ↔ Mainframe ───────────────────────────────
        builder.Property(x => x.ServiceName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.OperationName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.HttpMethod).HasMaxLength(10).HasDefaultValue(string.Empty);
        builder.Property(x => x.BasePath).HasMaxLength(1000).HasDefaultValue(string.Empty);
        builder.Property(x => x.TargetTransaction).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.RequestSchema).HasDefaultValue(string.Empty);
        builder.Property(x => x.ResponseSchema).HasDefaultValue(string.Empty);

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
