using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Tenant.
/// Define tabela, índices e constraints para o aggregate root de organização.
/// O slug possui índice único para garantir identificação amigável sem colisões.
///
/// v1.4: Suporte a hierarquia organizacional (ParentTenantId, TenantType, LegalName, TaxId).
/// Backward-compatible: ParentTenantId nullable, TenantType default Organization.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("iam_tenants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => TenantId.From(value));

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        // ── Hierarquia organizacional (v1.4) ──────────────────────────────

        builder.Property(x => x.ParentTenantId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? TenantId.From(value.Value) : null);

        builder.Property(x => x.TenantType)
            .HasConversion<int>()
            .HasDefaultValue(TenantType.Organization)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasMaxLength(512);

        builder.Property(x => x.TaxId)
            .HasMaxLength(50);

        // Índice ignora computed property (IsRoot) — baseado diretamente na coluna.
        builder.Ignore(x => x.IsRoot);

        // ── Índices ───────────────────────────────────────────────────────

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("IX_iam_tenants_slug");

        // Índice para consulta de child tenants de um parent.
        builder.HasIndex(x => x.ParentTenantId)
            .HasDatabaseName("IX_iam_tenants_parent");

        // Índice para filtro por tipo organizacional.
        builder.HasIndex(x => x.TenantType)
            .HasDatabaseName("IX_iam_tenants_type");

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
