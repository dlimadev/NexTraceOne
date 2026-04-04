using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.SuggestRunbooksForIncident;

/// <summary>
/// Feature: SuggestRunbooksForIncident — recomenda runbooks relevantes para um incidente
/// com base no serviço impactado, tipo de incidente e pesquisa textual.
/// Pontuação de relevância: match exato de tipo + serviço > match parcial.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SuggestRunbooksForIncident
{
    /// <summary>Query para sugerir runbooks para um incidente.</summary>
    public sealed record Query(
        string? ServiceId,
        string? IncidentType,
        string? IncidentTitle,
        int MaxResults = 5) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 20);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.IncidentType).MaximumLength(200).When(x => x.IncidentType is not null);
            RuleFor(x => x.IncidentTitle).MaximumLength(500).When(x => x.IncidentTitle is not null);
        }
    }

    /// <summary>
    /// Handler que pesquisa e pontua runbooks relevantes para o contexto do incidente.
    /// Critérios de relevância: serviço vinculado, tipo de incidente vinculado, match textual.
    /// </summary>
    public sealed class Handler(IRunbookRepository runbookRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Buscar runbooks candidatos: por serviço, por tipo, e por pesquisa textual
            var allCandidates = new List<ScoredRunbook>();

            // 1. Runbooks vinculados ao serviço + tipo (match exato)
            if (request.ServiceId is not null || request.IncidentType is not null)
            {
                var byServiceAndType = await runbookRepository.ListAsync(
                    request.ServiceId, request.IncidentType, null, cancellationToken);

                foreach (var rb in byServiceAndType)
                {
                    var score = ComputeRelevanceScore(rb.LinkedService, rb.LinkedIncidentType,
                        request.ServiceId, request.IncidentType);
                    allCandidates.Add(new ScoredRunbook(rb.Id.Value, rb.Title, rb.Description,
                        rb.LinkedService, rb.LinkedIncidentType, score));
                }
            }

            // 2. Runbooks por pesquisa textual no título do incidente
            if (request.IncidentTitle is not null)
            {
                var bySearch = await runbookRepository.ListAsync(
                    null, null, request.IncidentTitle, cancellationToken);

                foreach (var rb in bySearch)
                {
                    // Evitar duplicatas já adicionadas pelo match de serviço/tipo
                    if (allCandidates.Exists(c => c.RunbookId == rb.Id.Value))
                        continue;

                    allCandidates.Add(new ScoredRunbook(rb.Id.Value, rb.Title, rb.Description,
                        rb.LinkedService, rb.LinkedIncidentType, 0.3m));
                }
            }

            // 3. Se nenhum candidato encontrado, retornar runbooks genéricos (sem filtro)
            if (allCandidates.Count == 0)
            {
                var generic = await runbookRepository.ListAsync(null, null, null, cancellationToken);
                foreach (var rb in generic.Take(request.MaxResults))
                {
                    allCandidates.Add(new ScoredRunbook(rb.Id.Value, rb.Title, rb.Description,
                        rb.LinkedService, rb.LinkedIncidentType, 0.1m));
                }
            }

            // Ordenar por relevância e limitar resultados
            var suggestions = allCandidates
                .OrderByDescending(s => s.RelevanceScore)
                .Take(request.MaxResults)
                .Select(s => new RunbookSuggestionDto(
                    s.RunbookId, s.Title, s.Description,
                    s.LinkedService, s.LinkedIncidentType,
                    s.RelevanceScore,
                    GetMatchReason(s.RelevanceScore)))
                .ToList()
                .AsReadOnly();

            return new Response(suggestions);
        }

        private static decimal ComputeRelevanceScore(
            string? rbService, string? rbType,
            string? queryService, string? queryType)
        {
            var score = 0m;

            // Serviço exacto: +0.5
            if (rbService is not null && queryService is not null &&
                string.Equals(rbService, queryService, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.5m;
            }

            // Tipo de incidente exacto: +0.4
            if (rbType is not null && queryType is not null &&
                string.Equals(rbType, queryType, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.4m;
            }

            // Bonus: runbook tem serviço definido (mais específico que genérico): +0.1
            if (rbService is not null && score > 0)
            {
                score += 0.1m;
            }

            return score > 0 ? score : 0.2m; // Mínimo de 0.2 para candidatos parciais
        }

        private static string GetMatchReason(decimal score)
        {
            return score switch
            {
                >= 0.9m => "exact_service_and_type_match",
                >= 0.5m => "service_match",
                >= 0.4m => "type_match",
                >= 0.3m => "text_search_match",
                _ => "general_recommendation"
            };
        }

        private sealed record ScoredRunbook(
            Guid RunbookId,
            string Title,
            string? Description,
            string? LinkedService,
            string? LinkedIncidentType,
            decimal RelevanceScore);
    }

    /// <summary>Sugestão de runbook com score de relevância.</summary>
    public sealed record RunbookSuggestionDto(
        Guid RunbookId,
        string Title,
        string? Description,
        string? LinkedService,
        string? LinkedIncidentType,
        decimal RelevanceScore,
        string MatchReason);

    /// <summary>Resposta com runbooks sugeridos.</summary>
    public sealed record Response(IReadOnlyList<RunbookSuggestionDto> Suggestions);
}
