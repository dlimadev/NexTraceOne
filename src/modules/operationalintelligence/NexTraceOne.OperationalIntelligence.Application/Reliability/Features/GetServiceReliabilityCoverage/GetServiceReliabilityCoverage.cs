using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;

/// <summary>
/// Feature: GetServiceReliabilityCoverage — obtém os indicadores de cobertura
/// e prontidão operacional de um serviço. Indica se o serviço tem sinais,
/// runbooks, ownership, dependências mapeadas, contexto de mudanças e linkage
/// com incidentes. Relevante para Platform Admin e Auditor.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceReliabilityCoverage
{
    /// <summary>Query para obter cobertura operacional de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe os indicadores de cobertura operacional do serviço.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = request.ServiceId.ToLowerInvariant() switch
            {
                "svc-order-api" => new Response("svc-order-api", true, true, true, true, true, true),
                "svc-payment-gateway" => new Response("svc-payment-gateway", true, true, true, true, true, true),
                "svc-notification-worker" => new Response("svc-notification-worker", true, false, true, true, false, false),
                "svc-inventory-consumer" => new Response("svc-inventory-consumer", true, false, true, true, true, false),
                "svc-catalog-sync" => new Response("svc-catalog-sync", true, false, true, false, false, true),
                "svc-report-scheduler" => new Response("svc-report-scheduler", false, false, true, false, false, false),
                _ => new Response(request.ServiceId, false, false, false, false, false, false)
            };

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com indicadores de cobertura operacional do serviço.</summary>
    public sealed record Response(
        string ServiceId,
        bool HasOperationalSignals,
        bool HasRunbook,
        bool HasOwner,
        bool HasDependenciesMapped,
        bool HasRecentChangeContext,
        bool HasIncidentLinkage,
        bool IsSimulated = true,
        string DataSource = "demo");
}
