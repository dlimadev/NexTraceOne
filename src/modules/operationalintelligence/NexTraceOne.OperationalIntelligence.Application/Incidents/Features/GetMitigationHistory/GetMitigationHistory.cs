using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;

/// <summary>
/// Feature: GetMitigationHistory — retorna o histórico de auditoria de mitigação de um incidente,
/// incluindo ações executadas, resultados, evidências e validações.
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

    /// <summary>Handler que compõe o histórico de mitigação do incidente.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindHistory(request.IncidentId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindHistory(string incidentId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Entries: new[]
                    {
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10001-0001-0000-0000-000000000001"),
                            WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                            Action: "workflow-created",
                            PerformedBy: "ai-assistant",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                            Notes: "Workflow created based on AI-generated recommendations.",
                            Outcome: null,
                            ValidationResult: null,
                            LinkedEvidence: new[] { "deployment-diff:v2.13.2..v2.14.0", "error-rate-spike:14.2%" }),
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10001-0002-0000-0000-000000000002"),
                            WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                            Action: "approved",
                            PerformedBy: "tech-lead@nextraceone.io",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T10:30:00Z"),
                            Notes: "Approved based on correlation evidence and low risk of rollback.",
                            Outcome: null,
                            ValidationResult: null,
                            LinkedEvidence: Array.Empty<string>()),
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10001-0003-0000-0000-000000000003"),
                            WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                            Action: "rollback-triggered",
                            PerformedBy: "ops-engineer@nextraceone.io",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T10:36:00Z"),
                            Notes: "Rollback pipeline triggered for payment-service.",
                            Outcome: MitigationOutcome.Successful,
                            ValidationResult: "Deployment reverted successfully",
                            LinkedEvidence: new[] { "pipeline-run:12345" }),
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10001-0004-0000-0000-000000000004"),
                            WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                            Action: "step-completed",
                            PerformedBy: "ops-engineer@nextraceone.io",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T10:42:00Z"),
                            Notes: "Deployment verified via health check.",
                            Outcome: null,
                            ValidationResult: null,
                            LinkedEvidence: new[] { "health-check:payment-service:ok" }),
                    });
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Entries: new[]
                    {
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10002-0001-0000-0000-000000000001"),
                            WorkflowId: Guid.Parse("00000002-0001-0000-0000-000000000001"),
                            Action: "workflow-created",
                            PerformedBy: "ai-assistant",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T14:45:00Z"),
                            Notes: "Workflow created for external dependency verification.",
                            Outcome: null,
                            ValidationResult: null,
                            LinkedEvidence: new[] { "connection-timeout:catalog-sync-provider" }),
                        new MitigationAuditEntryDto(
                            EntryId: Guid.Parse("aad10002-0002-0000-0000-000000000002"),
                            WorkflowId: null,
                            Action: "recommendation-generated",
                            PerformedBy: "ai-assistant",
                            PerformedAt: DateTimeOffset.Parse("2024-06-15T14:46:00Z"),
                            Notes: "AI generated 2 mitigation recommendations for this incident.",
                            Outcome: null,
                            ValidationResult: null,
                            LinkedEvidence: Array.Empty<string>()),
                    });
            }

            return null;
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
