using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

internal sealed class ServiceLinkConfiguration : IEntityTypeConfiguration<ServiceLink>
{
    public void Configure(EntityTypeBuilder<ServiceLink> builder)
    {
        builder.ToTable("cat_service_links", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_service_links_category",
                "\"Category\" IN ('Repository', 'Documentation', 'CiCd', 'Monitoring', 'Wiki', 'SwaggerUi', 'ApiPortal', 'Backstage', 'Adr', 'Runbook', 'Changelog', 'Dashboard', 'Other')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceLinkId.From(value));

        builder.Property(x => x.ServiceAssetId)
            .HasConversion(id => id.Value, value => ServiceAssetId.From(value))
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.IconHint)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // ── Relação com ServiceAsset ──────────────────────────────────
        builder.HasOne(x => x.ServiceAsset)
            .WithMany()
            .HasForeignKey(x => x.ServiceAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.ServiceAssetId);
        builder.HasIndex(x => new { x.ServiceAssetId, x.Category });
    }
}
