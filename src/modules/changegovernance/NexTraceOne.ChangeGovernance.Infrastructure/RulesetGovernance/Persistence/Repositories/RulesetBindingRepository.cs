using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de bindings de ruleset para tipos de ativo.
/// </summary>
internal sealed class RulesetBindingRepository(RulesetGovernanceDbContext context)
    : RepositoryBase<RulesetBinding, RulesetBindingId>(context), IRulesetBindingRepository
{
    /// <summary>Busca um binding pelo identificador do ruleset e tipo de ativo.</summary>
    public async Task<RulesetBinding?> GetByRulesetAndAssetTypeAsync(
        RulesetId rulesetId, string assetType, CancellationToken cancellationToken = default)
        => await context.RulesetBindings
            .SingleOrDefaultAsync(b => b.RulesetId == rulesetId && b.AssetType == assetType, cancellationToken);
}
