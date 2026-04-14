using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo Cost Intelligence.
/// Garante que apenas o CostIntelligenceDbContext é commitado nos handlers de cost.
/// </summary>
public interface ICostIntelligenceUnitOfWork : IUnitOfWork;
