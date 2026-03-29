using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Unit of Work para o sub-domínio Legacy Assets.
/// Separado do CatalogGraphUnitOfWork para isolamento de bounded context.
/// </summary>
public interface ILegacyAssetsUnitOfWork : IUnitOfWork;
