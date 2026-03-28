using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;

/// <summary>
/// Feature: GetMitigationHistory — retorna o histórico de workflows de mitigação de um incidente,
/// lendo diretamente da base de dados via repositório dedicado.
/// </summary>
public static class GetMitigationHistory
{
    /// <summary>Query para obter o histórico de mitigação de um incidente.</summary>
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
    /// Handler que lê os workflows de mitigação do incidente via repositório,
    /// mapeando cada registo para uma entrada de auditoria.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IMitigationWorkflowRepository workflowRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.IncidentId, out var incidentGuid))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            if (!store.IncidentExists(request.IncidentId))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            var workflows = await workflowRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);

            var entries = workflows
                .Select(w => new MitigationAuditEntryDto(
                    EntryId: w.Id.Value,
                    WorkflowId: w.Id.Value,
                    Action: w.ActionType.ToString(),
                    PerformedBy: w.CreatedByUser,
                    PerformedAt: w.CreatedAt,
                    Notes: w.CompletedNotes,
                    Outcome: w.CompletedOutcome,
                    ValidationResult: w.Status.ToString(),
                    LinkedEvidence: []))
                .ToArray();

            return Result<Response>.Success(new Response(incidentGuid, entries));
        }
    }

    /// <summary>Resposta com o histórico de auditoria de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        IReadOnlyList<MitigationAuditEntryDto> Entries);

    /// <summary>Entrada individual no histórico de auditoria de mitigação.</summary>
    public sealed record MitigationAuditEntryDto(
        Guid EntryId,
        Guid? WorkflowId,
        string Action,
        string PerformedBy,
        DateTimeOffset PerformedAt,
        string? Notes,
        MitigationOutcome? Outcome,
        string? ValidationResult,
        IReadOnlyList<string> LinkedEvidence);
}
