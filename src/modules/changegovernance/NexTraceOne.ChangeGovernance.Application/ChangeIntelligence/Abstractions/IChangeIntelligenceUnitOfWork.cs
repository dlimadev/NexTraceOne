using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo ChangeIntelligence para garantir commit no ChangeIntelligenceDbContext.
/// </summary>
public interface IChangeIntelligenceUnitOfWork : IUnitOfWork;
