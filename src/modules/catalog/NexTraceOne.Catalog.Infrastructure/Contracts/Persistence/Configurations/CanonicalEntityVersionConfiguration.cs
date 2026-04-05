using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade CanonicalEntityVersion.
/// Versões imutáveis de schemas canónicos para histórico e cálculo de diff.
/// </summary>
internal sealed class CanonicalEntityVersionConfiguration : IEntityTypeConfiguration<CanonicalEntityVersion>
{
    public void Configure(EntityTypeBuilder<CanonicalEntityVersion> builder)
    {
        builder.ToTable("ctr_canonical_entity_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CanonicalEntityVersionId.From(value));

        builder.Property(x => x.CanonicalEntityId)
            .HasConversion(id => id.Value, value => CanonicalEntityId.From(value))
            .IsRequired();

        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SchemaContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.SchemaFormat).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ChangeDescription).HasMaxLength(2000);
        builder.Property(x => x.PublishedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.CanonicalEntityId);
        builder.HasIndex(x => new { x.CanonicalEntityId, x.Version }).IsUnique();
    }
}
