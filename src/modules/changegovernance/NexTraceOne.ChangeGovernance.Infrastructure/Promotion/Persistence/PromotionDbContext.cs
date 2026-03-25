using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;

/// <summary>
/// DbContext do módulo Promotion.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class PromotionDbContext(
    DbContextOptions<PromotionDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Ambientes de deployment persistidos no módulo Promotion.</summary>
    public DbSet<DeploymentEnvironment> DeploymentEnvironments => Set<DeploymentEnvironment>();

    /// <summary>Solicitações de promoção persistidas no módulo Promotion.</summary>
    public DbSet<PromotionRequest> PromotionRequests => Set<PromotionRequest>();

    /// <summary>Gates de promoção persistidos no módulo Promotion.</summary>
    public DbSet<PromotionGate> PromotionGates => Set<PromotionGate>();

    /// <summary>Avaliações de gate persistidas no módulo Promotion.</summary>
    public DbSet<GateEvaluation> GateEvaluations => Set<GateEvaluation>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(PromotionDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "chg_prm_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
