using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Identity.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Usuários persistidos do módulo Identity.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Papéis persistidos do módulo Identity.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Permissões persistidas do módulo Identity.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Sessões persistidas do módulo Identity.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>Vínculos de tenant persistidos do módulo Identity.</summary>
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IdentityDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
