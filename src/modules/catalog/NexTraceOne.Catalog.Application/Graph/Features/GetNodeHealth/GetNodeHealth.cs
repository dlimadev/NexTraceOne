using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Enums;

namespace NexTraceOne.EngineeringGraph.Application.Features.GetNodeHealth;

/// <summary>
/// Feature: GetNodeHealth — obtém dados de saúde/métricas para overlay explicável.
/// Retorna scores, status e fatores contribuintes para cada nó,
/// permitindo que a UI exiba overlays com explicação clara
/// de "por que este nó está neste estado".
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetNodeHealth
{
    /// <summary>Query de dados de saúde por modo de overlay.</summary>
    public sealed record Query(OverlayMode OverlayMode) : IQuery<Response>;

    /// <summary>Valida o modo de overlay solicitado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.OverlayMode).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que retorna dados de saúde dos nós para o overlay solicitado.
    /// Consulta o repositório de health records e projeta para a resposta da API.
    /// </summary>
    public sealed class Handler(INodeHealthRepository healthRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var records = await healthRepository.GetLatestByOverlayAsync(request.OverlayMode, cancellationToken);

            var items = records.Select(r => new NodeHealthItem(
                r.NodeId,
                r.NodeType.ToString(),
                r.Status.ToString(),
                r.Score,
                r.FactorsJson,
                r.CalculatedAt,
                r.SourceSystem)).ToList();

            return new Response(request.OverlayMode.ToString(), items);
        }
    }

    /// <summary>Resposta com dados de saúde para overlay.</summary>
    public sealed record Response(string OverlayMode, IReadOnlyList<NodeHealthItem> Items);

    /// <summary>Dados de saúde de um nó individual para o overlay.</summary>
    public sealed record NodeHealthItem(
        Guid NodeId,
        string NodeType,
        string Status,
        decimal Score,
        string FactorsJson,
        DateTimeOffset CalculatedAt,
        string SourceSystem);
}
