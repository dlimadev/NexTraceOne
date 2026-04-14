using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;

/// <summary>
/// Unit of work específico do módulo Ruleset Governance para evitar commits em DbContexts de outros módulos.
/// </summary>
public interface IRulesetGovernanceUnitOfWork : IUnitOfWork;
