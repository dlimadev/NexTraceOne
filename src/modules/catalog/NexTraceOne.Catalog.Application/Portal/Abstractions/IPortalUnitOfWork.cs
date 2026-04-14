using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Unit of work específico do módulo Portal para evitar commits em DbContexts de outros módulos.
/// </summary>
public interface IPortalUnitOfWork : IUnitOfWork;
