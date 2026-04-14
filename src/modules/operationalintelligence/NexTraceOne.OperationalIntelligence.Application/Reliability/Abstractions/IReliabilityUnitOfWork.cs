using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo Reliability.
/// Garante que apenas o ReliabilityDbContext é commitado nos handlers de reliability.
/// </summary>
public interface IReliabilityUnitOfWork : IUnitOfWork;
