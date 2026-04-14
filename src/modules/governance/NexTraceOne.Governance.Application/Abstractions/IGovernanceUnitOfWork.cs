using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Unit of work específico do módulo Governance para garantir commit no GovernanceDbContext.
/// </summary>
public interface IGovernanceUnitOfWork : IUnitOfWork;
