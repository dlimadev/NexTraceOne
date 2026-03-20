using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Unit of work específico do módulo Contracts para evitar commits em DbContexts de outros módulos.
/// </summary>
public interface IContractsUnitOfWork : IUnitOfWork;
