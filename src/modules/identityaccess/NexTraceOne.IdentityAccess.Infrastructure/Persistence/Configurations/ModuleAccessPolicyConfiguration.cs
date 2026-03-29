using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ModuleAccessPolicy.
/// Controlo granular de acesso a módulos, páginas e ações por papel e tenant.
/// Modelo enterprise para bancos e seguradoras que exigem granularidade
/// ao nível de ação individual no sistema.
/// </summary>
internal sealed class ModuleAccessPolicyConfiguration : IEntityTypeConfiguration<ModuleAccessPolicy>
{
    public void Configure(EntityTypeBuilder<ModuleAccessPolicy> builder)
    {
        builder.ToTable("iam_module_access_policies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ModuleAccessPolicyId.From(value));

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? TenantId.From(value.Value) : null);

        builder.Property(x => x.Module)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Page)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsAllowed).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy).HasMaxLength(200);

        // Índice composto: garante unicidade de política por role+tenant+module+page+action.
        builder.HasIndex(x => new { x.RoleId, x.TenantId, x.Module, x.Page, x.Action })
            .IsUnique()
            .HasDatabaseName("ix_iam_module_access_policies_role_tenant_module_page_action");

        // Índice de performance para consultas por role, tenant e module.
        builder.HasIndex(x => new { x.RoleId, x.TenantId, x.Module, x.IsActive })
            .HasDatabaseName("ix_iam_module_access_policies_role_tenant_module_active");
    }
}
