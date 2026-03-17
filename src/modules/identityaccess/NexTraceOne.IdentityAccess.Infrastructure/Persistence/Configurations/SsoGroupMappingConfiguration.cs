using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para SsoGroupMapping.
/// Mapeia associações entre grupos externos do SSO e roles internas por tenant.
/// Índice único por (TenantId, Provider, ExternalGroupId) evita mapeamentos duplicados.
/// </summary>
internal sealed class SsoGroupMappingConfiguration : IEntityTypeConfiguration<SsoGroupMapping>
{
    public void Configure(EntityTypeBuilder<SsoGroupMapping> builder)
    {
        builder.ToTable("identity_sso_group_mappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SsoGroupMappingId.From(value));

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExternalGroupId)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ExternalGroupName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Provider, x.ExternalGroupId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
