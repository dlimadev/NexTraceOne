using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.FindSimilarIncidents;

/// <summary>
/// Feature: FindSimilarIncidents — pesquisa incidentes semelhantes nos últimos N dias
/// baseada em critérios de similaridade: serviço, tipo, ambiente e padrão de correlação.
///
/// Algoritmo de similaridade:
///   - Mesmo serviço + mesmo tipo → similaridade alta
///   - Mesmo serviço + ambiente diferente → similaridade moderada
///   - Mesmo tipo + correlação com mesmo serviço → similaridade moderada
///   - Outros → similaridade baixa (incluídos se acima do threshold)
///
/// Permite ao engenheiro identificar padrões recorrentes e evitar re-trabalho de diagnóstico.
///
/// Valor: "incidentes semelhantes nos últimos 90 dias" — reduz MTTR por reutilização de contexto.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class FindSimilarIncidents
{
    private const int DefaultLookbackDays = 90;

    /// <summary>Query para pesquisar incidentes semelhantes.</summary>
    public sealed record Query(
        string IncidentId,
        int LookbackDays = DefaultLookbackDays,
        int MaxResults = 10) : IQuery<Response>;

    /// <summary>Valida os parâmetros da pesquisa.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que pesquisa incidentes similares baseado nos atributos do incidente raiz.</summary>
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            var cutoff = detail.Identity.CreatedAt.AddDays(-request.LookbackDays);
            var targetServiceId = detail.LinkedServices.FirstOrDefault()?.ServiceId ?? string.Empty;
            var targetEnvironment = detail.ImpactedEnvironment;
            var targetIncidentType = detail.Identity.IncidentType;

            var allIncidents = store.GetIncidentListItems()
                .Where(i => i.CreatedAt >= cutoff
                         && !i.IncidentId.ToString().Equals(request.IncidentId, StringComparison.OrdinalIgnoreCase))
                .Select(i => new IncidentCandidate(
                    i.IncidentId, i.Reference, i.Title, i.IncidentType, i.Severity,
                    i.ServiceId, i.ServiceDisplayName, i.Environment, i.CreatedAt))
                .ToList();

            var scored = allIncidents
                .Select(candidate => ScoreSimilarity(candidate, targetIncidentType, targetServiceId, targetEnvironment))
                .Where(s => s.SimilarityScore > 0)
                .OrderByDescending(s => s.SimilarityScore)
                .Take(request.MaxResults)
                .ToList();

            var pattern = DetermineRecurrencePattern(scored, targetIncidentType);

            return Task.FromResult(Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                LookbackDays: request.LookbackDays,
                SimilarIncidentCount: scored.Count,
                SimilarIncidents: scored.AsReadOnly(),
                RecurrencePattern: pattern,
                HasRecurringPattern: scored.Count >= 2)));
        }

        private static SimilarIncidentItem ScoreSimilarity(
            IncidentCandidate candidate,
            IncidentType targetIncidentType,
            string targetServiceId,
            string? targetEnvironment)
        {
            var score = 0;
            var reasons = new List<string>();

            var sameService = candidate.ServiceId.Equals(targetServiceId, StringComparison.OrdinalIgnoreCase);
            var sameType = candidate.IncidentType == targetIncidentType;
            var sameEnvironment = candidate.Environment.Equals(
                targetEnvironment ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            if (sameService && sameType)
            {
                score += 3;
                reasons.Add("Same service and incident type.");
            }
            else if (sameService)
            {
                score += 2;
                reasons.Add("Same service, different incident type.");
            }
            else if (sameType)
            {
                score += 1;
                reasons.Add("Same incident type, different service.");
            }

            if (sameEnvironment) score += 1;

            if (score == 0) return new SimilarIncidentItem(
                candidate.IncidentId, candidate.Reference, candidate.Title,
                candidate.IncidentType, candidate.Severity, candidate.ServiceId,
                candidate.ServiceDisplayName, candidate.Environment, candidate.CreatedAt,
                0, "None", string.Empty);

            var classification = score switch
            {
                >= 4 => "High",
                >= 2 => "Medium",
                _ => "Low"
            };

            return new SimilarIncidentItem(
                candidate.IncidentId,
                candidate.Reference,
                candidate.Title,
                candidate.IncidentType,
                candidate.Severity,
                candidate.ServiceId,
                candidate.ServiceDisplayName,
                candidate.Environment,
                candidate.CreatedAt,
                score,
                classification,
                string.Join(" ", reasons));
        }

        private static string DetermineRecurrencePattern(
            IReadOnlyList<SimilarIncidentItem> similar,
            IncidentType type)
        {
            if (similar.Count == 0) return "No pattern detected.";
            if (similar.Count >= 5) return $"Recurring pattern detected: {similar.Count} similar incidents. High recurrence risk for '{type}' incidents. Consider permanent fix.";
            if (similar.Count >= 2) return $"Pattern emerging: {similar.Count} similar incidents found. Review root cause from previous incidents to avoid recurrence.";
            return "Isolated recurrence. Review previous incident for reusable remediation steps.";
        }
    }

    /// <summary>Incidente similar encontrado.</summary>
    public sealed record SimilarIncidentItem(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType Type,
        IncidentSeverity Severity,
        string ServiceId,
        string ServiceName,
        string Environment,
        DateTimeOffset CreatedAt,
        int SimilarityScore,
        string SimilarityClassification,
        string SimilarityReason);

    /// <summary>Resposta da pesquisa de incidentes similares.</summary>
    public sealed record Response(
        string IncidentId,
        int LookbackDays,
        int SimilarIncidentCount,
        IReadOnlyList<SimilarIncidentItem> SimilarIncidents,
        string RecurrencePattern,
        bool HasRecurringPattern);

    private sealed record IncidentCandidate(
        Guid IncidentId,
        string Reference,
        string Title,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        string ServiceId,
        string ServiceDisplayName,
        string Environment,
        DateTimeOffset CreatedAt);
}
