using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade RolePermission.
/// Tabela de junção que liga Role→Permission com suporte a personalização por tenant.
/// Índice composto (RoleId, PermissionCode, TenantId) garante unicidade de mapeamentos.
/// </summary>
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("iam_role_permissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RolePermissionId.From(value));

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();

        builder.Property(x => x.PermissionCode)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? TenantId.From(value.Value) : null);

        builder.Property(x => x.GrantedAt).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();

        // Índice composto: garante que não existem duplicatas de role+permission+tenant.
        // TenantId nulo = padrão do sistema, TenantId preenchido = override do tenant.
        builder.HasIndex(x => new { x.RoleId, x.PermissionCode, x.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_iam_role_permissions_role_perm_tenant");

        // Índice de performance para consultas por role e tenant ativo.
        builder.HasIndex(x => new { x.RoleId, x.TenantId, x.IsActive })
            .HasDatabaseName("ix_iam_role_permissions_role_tenant_active");
    }
}
