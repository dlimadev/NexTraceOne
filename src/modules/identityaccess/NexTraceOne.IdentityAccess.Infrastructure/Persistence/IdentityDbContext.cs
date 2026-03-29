using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Domain.Entities;

using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Identity.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
///
/// Entidades gerenciadas:
/// - v1.0: User, Role, Permission, Session, TenantMembership
/// - v1.1: ExternalIdentity, SsoGroupMapping, BreakGlassRequest, JitAccessRequest,
///          Delegation, AccessReviewCampaign, AccessReviewItem, SecurityEvent
/// - v1.2: Environment, EnvironmentAccess
/// - v1.3: RolePermission, ModuleAccessPolicy
/// </summary>
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    // ── v1.0 Core ────────────────────────────────────────────────────────

    /// <summary>Tenants (organizações/clientes) persistidos do módulo Identity.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

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

    // ── v1.1 Enterprise — SSO / Federation ───────────────────────────────

    /// <summary>Identidades externas vinculadas a usuários internos (OIDC, SAML, SCIM).</summary>
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    /// <summary>Mapeamentos de grupos SSO para roles internas.</summary>
    public DbSet<SsoGroupMapping> SsoGroupMappings => Set<SsoGroupMapping>();

    // ── v1.1 Enterprise — Privileged Access ──────────────────────────────

    /// <summary>Solicitações de acesso emergencial (Break Glass).</summary>
    public DbSet<BreakGlassRequest> BreakGlassRequests => Set<BreakGlassRequest>();

    /// <summary>Solicitações de acesso privilegiado temporário (JIT).</summary>
    public DbSet<JitAccessRequest> JitAccessRequests => Set<JitAccessRequest>();

    /// <summary>Delegações formais de permissões entre usuários.</summary>
    public DbSet<Delegation> Delegations => Set<Delegation>();

    // ── v1.1 Enterprise — Access Review ──────────────────────────────────

    /// <summary>Campanhas de recertificação periódica de acessos.</summary>
    public DbSet<AccessReviewCampaign> AccessReviewCampaigns => Set<AccessReviewCampaign>();

    /// <summary>Itens individuais de revisão de acesso.</summary>
    public DbSet<AccessReviewItem> AccessReviewItems => Set<AccessReviewItem>();

    // ── v1.1 Enterprise — Session Intelligence ───────────────────────────

    /// <summary>Eventos de segurança e anomalias detectadas.</summary>
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

    // ── v1.2 — Autorização por Ambiente ──────────────────────────────────

    /// <summary>Ambientes do ciclo de vida (Development, Pre-Production, Production) por tenant.</summary>
    public DbSet<Environment> Environments { get; init; } = null!;

    /// <summary>Acessos granulares de usuários a ambientes específicos dentro de um tenant.</summary>
    public DbSet<EnvironmentAccess> EnvironmentAccesses { get; init; } = null!;

    // ── v1.3 — Autorização Granular Enterprise ────────────────────────────

    /// <summary>Mapeamentos papel→permissão persistidos, com suporte a personalização por tenant.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>Políticas de acesso granular por módulo/página/ação por papel e tenant.</summary>
    public DbSet<ModuleAccessPolicy> ModuleAccessPolicies => Set<ModuleAccessPolicy>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IdentityDbContext).Assembly;

    /// <inheritdoc />
    protected override string OutboxTableName => "iam_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
