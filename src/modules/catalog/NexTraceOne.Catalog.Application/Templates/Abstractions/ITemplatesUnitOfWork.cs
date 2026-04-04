using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.Templates.Abstractions;

/// <summary>
/// Unit of work específico do módulo Templates para evitar commits em DbContexts de outros módulos.
/// </summary>
public interface ITemplatesUnitOfWork : IUnitOfWork;
