using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Unidade de trabalho dedicada ao Catalog Graph.
/// Permite que fluxos cross-context persistam ApiAssets/ServiceAssets reais sem depender de resoluções ambíguas de IUnitOfWork.
/// </summary>
public interface ICatalogGraphUnitOfWork : IUnitOfWork
{
}
