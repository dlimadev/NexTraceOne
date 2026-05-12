using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceInterfaceById;

/// <summary>
/// Feature: GetServiceInterfaceById — obtém o detalhe completo de uma interface de serviço.
/// Estrutura VSA: Query + Handler + Response em ficheiro único.
/// </summary>
public static class GetServiceInterfaceById
{
    /// <summary>Consulta de detalhe de interface por identificador.</summary>
    public sealed record Query(Guid InterfaceId) : IQuery<Response?>;

    /// <summary>Handler que obtém o detalhe de uma interface de serviço.</summary>
    public sealed class Handler(
        IServiceInterfaceRepository serviceInterfaceRepository) : IQueryHandler<Query, Response?>
    {
        public async Task<Result<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            var iface = await serviceInterfaceRepository.GetByIdAsync(
                ServiceInterfaceId.From(request.InterfaceId),
                cancellationToken);

            if (iface is null)
                return (Response?)null;

            return new Response(
                iface.Id.Value,
                iface.ServiceAssetId.Value,
                iface.Name,
                iface.Description,
                iface.InterfaceType.ToString(),
                iface.Status.ToString(),
                iface.ExposureScope.ToString(),
                iface.BasePath,
                iface.TopicName,
                iface.WsdlNamespace,
                iface.GrpcServiceName,
                iface.ScheduleCron,
                iface.EnvironmentId,
                iface.SloTarget,
                iface.RequiresContract,
                iface.AuthScheme.ToString(),
                iface.RateLimitPolicy,
                iface.DocumentationUrl,
                iface.DeprecationDate,
                iface.SunsetDate,
                iface.DeprecationNotice,
                iface.IsDeprecated,
                iface.CreatedAt,
                iface.UpdatedAt);
        }
    }

    /// <summary>Resposta de detalhe de interface de serviço.</summary>
    public sealed record Response(
        Guid InterfaceId,
        Guid ServiceAssetId,
        string Name,
        string Description,
        string InterfaceType,
        string Status,
        string ExposureScope,
        string BasePath,
        string TopicName,
        string WsdlNamespace,
        string GrpcServiceName,
        string ScheduleCron,
        string EnvironmentId,
        string SloTarget,
        bool RequiresContract,
        string AuthScheme,
        string RateLimitPolicy,
        string DocumentationUrl,
        DateTimeOffset? DeprecationDate,
        DateTimeOffset? SunsetDate,
        string? DeprecationNotice,
        bool IsDeprecated,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
