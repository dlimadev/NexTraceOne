using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;

/// <summary>
/// DbContext do módulo RulesetGovernance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class RulesetGovernanceDbContext(
    DbContextOptions<RulesetGovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Rulesets de governança persistidos no módulo RulesetGovernance.</summary>
    public DbSet<Ruleset> Rulesets => Set<Ruleset>();

    /// <summary>Bindings de ruleset para tipos de ativo.</summary>
    public DbSet<RulesetBinding> RulesetBindings => Set<RulesetBinding>();

    /// <summary>Resultados de execução de linting.</summary>
    public DbSet<LintResult> LintResults => Set<LintResult>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(RulesetGovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.RulesetGovernance.Infrastructure";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}
