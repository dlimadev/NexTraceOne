using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListServiceInterfaces;

/// <summary>
/// Feature: ListServiceInterfaces — lista todas as interfaces de um serviço específico.
/// Estrutura VSA: Query + Handler + Response em ficheiro único.
/// </summary>
public static class ListServiceInterfaces
{
    /// <summary>Consulta de listagem de interfaces de um serviço.</summary>
    public sealed record Query(Guid ServiceAssetId) : IQuery<IReadOnlyList<Response>>;

    /// <summary>Handler que lista as interfaces de um serviço.</summary>
    public sealed class Handler(
        IServiceInterfaceRepository serviceInterfaceRepository) : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<Result<IReadOnlyList<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var interfaces = await serviceInterfaceRepository.ListByServiceAsync(
                request.ServiceAssetId,
                cancellationToken);

            var result = interfaces
                .Select(i => new Response(
                    i.Id.Value,
                    i.Name,
                    i.InterfaceType.ToString(),
                    i.Status.ToString(),
                    i.ExposureScope.ToString(),
                    i.RequiresContract,
                    i.IsDeprecated))
                .ToList();

            return Result<IReadOnlyList<Response>>.Success(result);
        }
    }

    /// <summary>Resposta de listagem de interface de serviço.</summary>
    public sealed record Response(
        Guid InterfaceId,
        string Name,
        string InterfaceType,
        string Status,
        string ExposureScope,
        bool RequiresContract,
        bool IsDeprecated);
}
