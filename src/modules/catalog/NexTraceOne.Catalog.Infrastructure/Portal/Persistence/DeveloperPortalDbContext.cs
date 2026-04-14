using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence;

/// <summary>
/// DbContext do módulo DeveloperPortal.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class DeveloperPortalDbContext(
    DbContextOptions<DeveloperPortalDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IPortalUnitOfWork
{
    /// <summary>Subscrições de notificações de API.</summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>Sessões de execução no playground sandbox.</summary>
    public DbSet<PlaygroundSession> PlaygroundSessions => Set<PlaygroundSession>();

    /// <summary>Registos de geração de código a partir de contratos.</summary>
    public DbSet<CodeGenerationRecord> CodeGenerationRecords => Set<CodeGenerationRecord>();

    /// <summary>Eventos de analytics de utilização do portal.</summary>
    public DbSet<PortalAnalyticsEvent> PortalAnalyticsEvents => Set<PortalAnalyticsEvent>();

    /// <summary>Pesquisas salvas no catálogo.</summary>
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();

    /// <summary>Entradas do Publication Center — governa a exposição de contratos no Developer Portal.</summary>
    public DbSet<ContractPublicationEntry> ContractPublications => Set<ContractPublicationEntry>();

    /// <summary>API Keys para acesso programático ao Developer Portal.</summary>
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>Políticas de rate limiting por API.</summary>
    public DbSet<RateLimitPolicy> RateLimitPolicies => Set<RateLimitPolicy>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(DeveloperPortalDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "cat_portal_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
