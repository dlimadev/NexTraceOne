using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade UserRoleAssignment.
/// Tabela que vincula User→Tenant→Role com suporte a múltiplos papéis por tenant.
///
/// Índice composto (UserId, TenantId, RoleId) garante que o mesmo papel
/// não seja atribuído duas vezes ao mesmo usuário no mesmo tenant.
///
/// Índice de performance (UserId, TenantId, IsActive) para consultas frequentes.
/// </summary>
internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("iam_user_role_assignments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserRoleAssignmentId.From(value));

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();

        builder.Property(x => x.AssignedAt).IsRequired();

        builder.Property(x => x.AssignedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.ValidFrom);
        builder.Property(x => x.ValidUntil);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Índice composto único: mesmo papel não duplica para o mesmo user/tenant.
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.RoleId })
            .IsUnique()
            .HasDatabaseName("ix_iam_user_role_assignments_user_tenant_role");

        // Índice de performance para consultas por user e tenant ativos.
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.IsActive })
            .HasDatabaseName("ix_iam_user_role_assignments_user_tenant_active");

        // Índice para consultas por tenant (listagem de membros).
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_iam_user_role_assignments_tenant");
    }
}
