using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Repositories;

/// <summary>
/// Repositorio de rulesets, implementando consultas específicas de negócio.
/// </summary>
internal sealed class RulesetRepository(RulesetGovernanceDbContext context)
    : RepositoryBase<Ruleset, RulesetId>(context), IRulesetRepository
{
    /// <summary>Busca um Ruleset pelo seu identificador.</summary>
    public override async Task<Ruleset?> GetByIdAsync(RulesetId id, CancellationToken ct = default)
        => await context.Rulesets
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Lista rulesets ativos com paginação.</summary>
    public async Task<IReadOnlyList<Ruleset>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Rulesets
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.RulesetCreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Conta o total de rulesets ativos.</summary>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await context.Rulesets
            .CountAsync(r => !r.IsDeleted, cancellationToken);

    /// <summary>Busca um Ruleset pelo nome (para idempotência no marketplace).</summary>
    public async Task<Ruleset?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        => await context.Rulesets
            .FirstOrDefaultAsync(r => r.Name == name && !r.IsDeleted, cancellationToken);
}
