using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;

/// <summary>
/// Feature: GetDomainReliabilitySummary — obtém o resumo agregado de confiabilidade
/// dos serviços de um domínio de negócio. Similar ao resumo por equipa, mas agrupado
/// por domínio para visão de Architect e Executive.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetDomainReliabilitySummary
{
    /// <summary>Query para obter resumo de confiabilidade por domínio.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DomainId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe o resumo agregado de confiabilidade do domínio.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = request.DomainId.ToLowerInvariant() switch
            {
                "orders" => new Response("Orders", 2, 2, 0, 0, 0, 0),
                "payments" => new Response("Payments", 1, 0, 1, 0, 0, 1),
                "identity" => new Response("Identity", 2, 2, 0, 0, 0, 0),
                "catalog" => new Response("Catalog", 1, 0, 0, 1, 0, 0),
                "notifications" => new Response("Notifications", 1, 1, 0, 0, 0, 0),
                "analytics" => new Response("Analytics", 1, 0, 0, 0, 1, 0),
                _ => new Response(request.DomainId, 0, 0, 0, 0, 0, 0)
            };

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resumo agregado de confiabilidade do domínio.</summary>
    public sealed record Response(
        string DomainId,
        int TotalServices,
        int HealthyServices,
        int DegradedServices,
        int UnavailableServices,
        int NeedsAttentionServices,
        int CriticalServicesImpacted,
        bool IsSimulated = true,
        string DataSource = "demo");
}
