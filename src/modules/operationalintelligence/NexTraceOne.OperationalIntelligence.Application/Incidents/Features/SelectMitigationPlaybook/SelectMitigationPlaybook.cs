using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.SelectMitigationPlaybook;

/// <summary>
/// Feature: SelectMitigationPlaybook — seleciona automaticamente o runbook mais adequado
/// para mitigar um incidente com base no resultado do triage, no serviço afetado,
/// no tipo de incidente e na disponibilidade de runbooks associados.
///
/// Algoritmo de seleção:
///   1. Obtém o contexto do incidente (serviço, tipo, severidade, correlações)
///   2. Consulta runbooks elegíveis via IRunbookRepository (por serviço + tipo)
///   3. Calcula score de adequação para cada runbook candidato
///   4. Seleciona o runbook com maior score como playbook recomendado
///   5. Deriva contexto de execução (urgência, passos críticos de arranque)
///
/// Integra com Triage (Phase 3.4) e SuggestRunbooksForIncident para fornecer
/// um playbook acionável em vez de apenas uma lista de sugestões.
///
/// Valor: reduz o MTTM (Mean Time to Mitigate) ao eliminar a escolha manual de runbook.
/// Persona primária: Engineer, Tech Lead.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SelectMitigationPlaybook
{
    /// <summary>Query para obter o playbook de mitigação automático de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que combina contexto de triage com runbooks disponíveis para selecionar
    /// o playbook de mitigação mais adequado ao incidente.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IRunbookRepository runbookRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!store.IncidentExists(request.IncidentId))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            var detail = store.GetIncidentDetail(request.IncidentId);
            if (detail is null)
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            var correlation = store.GetIncidentCorrelation(request.IncidentId);
            var severity = detail.Identity.Severity;
            var incidentType = detail.Identity.IncidentType;
            var serviceId = detail.LinkedServices.FirstOrDefault()?.ServiceId;
            var incidentTitle = detail.Identity.Title;

            // Buscar runbooks por serviço e tipo de incidente
            var candidates = await runbookRepository.ListAsync(
                serviceId, incidentType.ToString(), null, cancellationToken);

            // Fallback: se não há candidatos específicos, buscar por tipo apenas
            if (candidates.Count == 0 && serviceId is not null)
            {
                candidates = await runbookRepository.ListAsync(
                    null, incidentType.ToString(), null, cancellationToken);
            }

            // Fallback: se ainda não há candidatos, buscar por pesquisa textual
            if (candidates.Count == 0)
            {
                candidates = await runbookRepository.ListAsync(
                    null, null, incidentTitle, cancellationToken);
            }

            if (candidates.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    IncidentId: request.IncidentId,
                    PlaybookFound: false,
                    SelectedRunbookId: null,
                    SelectedRunbookTitle: null,
                    SelectedRunbookDescription: null,
                    SelectionRationale: "No matching runbook found for this incident type and service. Manual runbook selection required.",
                    ExecutionUrgency: MapUrgency(severity),
                    ExecutionContext: BuildExecutionContext(severity, correlation?.RelatedChanges?.Count ?? 0),
                    AlternativeRunbookIds: Array.Empty<Guid>()));
            }

            // Score cada candidato: serviço exato + tipo exato > tipo apenas > pesquisa
            var scored = candidates
                .Select(rb => new
                {
                    Runbook = rb,
                    Score = ComputeScore(rb.LinkedService, rb.LinkedIncidentType, serviceId, incidentType.ToString())
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var selected = scored[0].Runbook;
            var alternatives = scored.Skip(1).Take(3).Select(x => x.Runbook.Id.Value).ToArray();

            return Result<Response>.Success(new Response(
                IncidentId: request.IncidentId,
                PlaybookFound: true,
                SelectedRunbookId: selected.Id.Value,
                SelectedRunbookTitle: selected.Title,
                SelectedRunbookDescription: selected.Description,
                SelectionRationale: BuildSelectionRationale(
                    selected.LinkedService, selected.LinkedIncidentType, serviceId, incidentType.ToString()),
                ExecutionUrgency: MapUrgency(severity),
                ExecutionContext: BuildExecutionContext(severity, correlation?.RelatedChanges?.Count ?? 0),
                AlternativeRunbookIds: alternatives));
        }

        private static decimal ComputeScore(
            string? rbService, string? rbType,
            string? queryService, string? queryType)
        {
            var score = 0m;

            if (rbService is not null && queryService is not null &&
                string.Equals(rbService, queryService, StringComparison.OrdinalIgnoreCase))
                score += 0.5m;

            if (rbType is not null && queryType is not null &&
                string.Equals(rbType, queryType, StringComparison.OrdinalIgnoreCase))
                score += 0.4m;

            return score > 0 ? score : 0.1m;
        }

        private static string BuildSelectionRationale(
            string? rbService, string? rbType,
            string? queryService, string? queryType)
        {
            var hasServiceMatch = rbService is not null && queryService is not null &&
                string.Equals(rbService, queryService, StringComparison.OrdinalIgnoreCase);
            var hasTypeMatch = rbType is not null && queryType is not null &&
                string.Equals(rbType, queryType, StringComparison.OrdinalIgnoreCase);

            return (hasServiceMatch, hasTypeMatch) switch
            {
                (true, true) => $"Exact match: runbook linked to service '{queryService}' and incident type '{queryType}'.",
                (true, false) => $"Service match: runbook linked to service '{queryService}'.",
                (false, true) => $"Type match: runbook linked to incident type '{queryType}'.",
                _ => "Best available runbook by text search relevance."
            };
        }

        private static string MapUrgency(IncidentSeverity severity) => severity switch
        {
            IncidentSeverity.Critical => "Immediate",
            IncidentSeverity.Major => "High",
            IncidentSeverity.Minor => "Medium",
            _ => "Low"
        };

        private static IReadOnlyList<string> BuildExecutionContext(
            IncidentSeverity severity, int relatedChangesCount)
        {
            var context = new List<string>();

            if (severity is IncidentSeverity.Critical)
            {
                context.Add("CRITICAL: Execute immediately. Engage on-call lead.");
                context.Add("Activate P1 war-room protocol if not already active.");
            }
            else if (severity is IncidentSeverity.Major)
            {
                context.Add("HIGH: Notify owning team. Begin mitigation within 15 minutes.");
            }

            if (relatedChangesCount > 0)
            {
                context.Add($"{relatedChangesCount} correlated change(s) detected. Evaluate rollback as first mitigation step.");
            }

            context.Add("Document mitigation actions in the incident timeline.");
            context.Add("Validate service health after each mitigation step.");

            return context.AsReadOnly();
        }
    }

    /// <summary>Resposta com o playbook de mitigação automaticamente selecionado.</summary>
    public sealed record Response(
        string IncidentId,
        bool PlaybookFound,
        Guid? SelectedRunbookId,
        string? SelectedRunbookTitle,
        string? SelectedRunbookDescription,
        string SelectionRationale,
        string ExecutionUrgency,
        IReadOnlyList<string> ExecutionContext,
        IReadOnlyList<Guid> AlternativeRunbookIds);
}
