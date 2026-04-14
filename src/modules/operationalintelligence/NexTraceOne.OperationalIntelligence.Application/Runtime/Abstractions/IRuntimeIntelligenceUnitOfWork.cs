using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Unit of work específico do sub-módulo Runtime Intelligence.
/// Garante que apenas o RuntimeIntelligenceDbContext é commitado nos handlers de runtime.
/// </summary>
public interface IRuntimeIntelligenceUnitOfWork : IUnitOfWork;
