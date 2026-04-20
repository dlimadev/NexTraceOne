using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

using ListIncidentsFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents.ListIncidents;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendationsBySimilarity;

/// <summary>
/// Feature: GetMitigationRecommendationsBySimilarity — gera recomendações de mitigação
/// baseadas em similaridade com incidentes resolvidos anteriores.
///
/// Algoritmo:
///   1. Obtém os atributos do incidente alvo (serviço, tipo, ambiente)
///   2. Encontra incidentes resolvidos com sobreposição de serviço ou tipo
///   3. Pontua cada candidato usando a mesma lógica de FindSimilarIncidents
///   4. Gera sugestões de mitigação referenciando cada incidente similar resolvido
///
/// Valor: reduz MTTR ao reutilizar contexto de resoluções anteriores bem-sucedidas.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetMitigationRecommendationsBySimilarity
{
    private const int DefaultMaxResults = 5;

    /// <summary>Query para obter recomendações de mitigação por similaridade.</summary>
    public sealed record Query(
        string IncidentId,
        int MaxResults = DefaultMaxResults) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que computa as recomendações de mitigação por similaridade.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var targetServiceId = detail.LinkedServices.Count > 0
                ? detail.LinkedServices[0].ServiceId
                : string.Empty;
            var targetType = detail.Identity.IncidentType;
            var targetEnvironment = detail.ImpactedEnvironment;

            var resolvedIncidents = store.GetIncidentListItems()
                .Where(i => i.Status is IncidentStatus.Resolved or IncidentStatus.Closed
                         && !i.IncidentId.ToString().Equals(request.IncidentId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var scored = resolvedIncidents
                .Select(i => ScoreSimilarity(i, targetType, targetServiceId, targetEnvironment))
                .Where(s => s.Score > 0)
                .OrderByDescending(s => s.Score)
                .Take(request.MaxResults)
                .Select(s => new SimilarityRecommendation(
                    s.IncidentId,
                    s.Title,
                    s.Score,
                    $"Review resolved incident {s.IncidentId}. Apply runbook steps from previous resolution.",
                    []))
                .ToList();

            return Task.FromResult(Result<Response>.Success(new Response(
                request.IncidentId,
                scored.AsReadOnly())));
        }

        private static (Guid IncidentId, string Title, int Score) ScoreSimilarity(
            ListIncidentsFeature.IncidentListItem candidate,
            IncidentType targetType,
            string targetServiceId,
            string? targetEnvironment)
        {
            var score = 0;
            var sameService = candidate.ServiceId.Equals(targetServiceId, StringComparison.OrdinalIgnoreCase);
            var sameType = candidate.IncidentType == targetType;
            var sameEnvironment = candidate.Environment.Equals(
                targetEnvironment ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            if (sameService && sameType)
                score += 3;
            else if (sameService)
                score += 2;
            else if (sameType)
                score += 1;

            if (sameEnvironment) score += 1;

            return (candidate.IncidentId, candidate.Title, score);
        }
    }

    /// <summary>Recomendação de mitigação baseada em incidente similar resolvido.</summary>
    public sealed record SimilarityRecommendation(
        Guid ReferenceIncidentId,
        string ReferenceTitle,
        int SimilarityScore,
        string SuggestedMitigationSummary,
        IReadOnlyList<Guid> RecommendedRunbookIds);

    /// <summary>Resposta com lista de recomendações de mitigação por similaridade.</summary>
    public sealed record Response(
        string IncidentId,
        IReadOnlyList<SimilarityRecommendation> Recommendations);
}
