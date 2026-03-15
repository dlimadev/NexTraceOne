using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;

/// <summary>
/// Feature: GetTeamReliabilitySummary — obtém o resumo agregado de confiabilidade
/// dos serviços de uma equipa. Retorna contagens por estado, serviços críticos
/// impactados e indicadores de atenção.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetTeamReliabilitySummary
{
    /// <summary>Query para obter resumo de confiabilidade por equipa.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe o resumo agregado de confiabilidade da equipa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Simula dados agregados da equipa.
            var response = request.TeamId.ToLowerInvariant() switch
            {
                "order-squad" => new Response("order-squad", 3, 2, 0, 0, 1, 1),
                "payment-squad" => new Response("payment-squad", 1, 0, 1, 0, 0, 1),
                "platform-squad" => new Response("platform-squad", 2, 1, 0, 1, 0, 1),
                "identity-squad" => new Response("identity-squad", 2, 2, 0, 0, 0, 0),
                "data-squad" => new Response("data-squad", 1, 0, 0, 0, 1, 0),
                _ => new Response(request.TeamId, 0, 0, 0, 0, 0, 0)
            };

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resumo agregado de confiabilidade da equipa.</summary>
    public sealed record Response(
        string TeamId,
        int TotalServices,
        int HealthyServices,
        int DegradedServices,
        int UnavailableServices,
        int NeedsAttentionServices,
        int CriticalServicesImpacted);
}
