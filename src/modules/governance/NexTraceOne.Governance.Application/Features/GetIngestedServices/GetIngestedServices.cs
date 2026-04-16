using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetIngestedServices;

/// <summary>
/// Feature: GetIngestedServices — lista os serviços com métricas OTEL ingeridas.
/// Usado pelo Change Intelligence para saber quais serviços têm dados observáveis.
/// </summary>
public static class GetIngestedServices
{
    /// <summary>Query para listar serviços com métricas ingeridas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que consulta IOtelMetricRepository.</summary>
    public sealed class Handler(IOtelMetricRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var services = await repository.GetDistinctServiceNamesAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                Services: services,
                Total: services.Count,
                RetrievedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Response com lista de serviços observados.</summary>
    public sealed record Response(
        IReadOnlyList<string> Services,
        int Total,
        DateTimeOffset RetrievedAt);
}
