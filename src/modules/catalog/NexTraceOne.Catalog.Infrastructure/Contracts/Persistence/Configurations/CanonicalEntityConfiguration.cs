using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade CanonicalEntity.
/// Entidades canónicas são schemas reutilizáveis que servem como fonte de verdade
/// para payloads e modelos partilhados entre contratos.
/// </summary>
internal sealed class CanonicalEntityConfiguration : IEntityTypeConfiguration<CanonicalEntity>
{
    public void Configure(EntityTypeBuilder<CanonicalEntity> builder)
    {
        builder.ToTable("ctr_canonical_entities", t =>
        {
            t.HasCheckConstraint(
                "CK_ctr_canonical_entities_state",
                "\"State\" IN ('Draft', 'Published', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CanonicalEntityId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Domain).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Owner).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();

        builder.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(CanonicalEntityState.Draft)
            .IsRequired();

        builder.Property(x => x.SchemaContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.SchemaFormat).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Aliases)
            .HasColumnType("text[]");

        builder.Property(x => x.Tags)
            .HasColumnType("text[]");

        builder.Property(x => x.Criticality).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReusePolicy).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OrganizationId).HasMaxLength(256);
        builder.Property(x => x.KnownUsageCount).IsRequired().HasDefaultValue(0);

        // Auditoria
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Domain);
        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");
    }
}
