using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

/// <summary>
/// Unit of work específico do módulo Dependency Governance para evitar commits em DbContexts de outros módulos.
/// </summary>
public interface IDependencyGovernanceUnitOfWork : IUnitOfWork;
