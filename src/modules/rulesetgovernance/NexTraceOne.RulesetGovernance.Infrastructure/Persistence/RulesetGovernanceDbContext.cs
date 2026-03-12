using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo RulesetGovernance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros modulos NUNCA referenciam este DbContext. Comunicacao via Integration Events.
/// </summary>
public sealed class RulesetGovernanceDbContext(
    DbContextOptions<RulesetGovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Rulesets de governanca persistidos no modulo RulesetGovernance.</summary>
    public DbSet<Ruleset> Rulesets => Set<Ruleset>();

    /// <summary>Bindings de ruleset para tipos de ativo.</summary>
    public DbSet<RulesetBinding> RulesetBindings => Set<RulesetBinding>();

    /// <summary>Resultados de execucao de linting.</summary>
    public DbSet<LintResult> LintResults => Set<LintResult>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(RulesetGovernanceDbContext).Assembly;

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
