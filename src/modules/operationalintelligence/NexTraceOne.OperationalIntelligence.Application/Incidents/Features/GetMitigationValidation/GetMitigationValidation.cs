using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;

/// <summary>
/// Feature: GetMitigationValidation — retorna o estado de validação pós-mitigação de um workflow,
/// incluindo verificações esperadas, resultado observado e resumo de sinais pós-mitigação.
/// </summary>
public static class GetMitigationValidation
{
    /// <summary>Query para obter a validação de um workflow de mitigação.</summary>
    public sealed record Query(string IncidentId, string WorkflowId) : IQuery<Response>;

    /// <summary>Valida os identificadores do incidente e do workflow.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe os dados de validação do workflow de mitigação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindValidation(request.IncidentId, request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindValidation(string incidentId, string workflowId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)
                && workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                    Status: ValidationStatus.InProgress,
                    ExpectedChecks: new[]
                    {
                        new ValidationCheckDto("Error rate below threshold", "Error rate should return to < 0.5% within 30 minutes", true, "0.3%"),
                        new ValidationCheckDto("Payment success rate recovered", "Payment success rate should be above 99.5%", true, "99.7%"),
                        new ValidationCheckDto("No new error patterns", "No new error types should appear post-rollback", true, null),
                        new ValidationCheckDto("Downstream consumers healthy", "All downstream services report healthy status", false, "2 of 3 confirmed"),
                    },
                    ObservedOutcome: "Error rate recovered to baseline. Payment success rate at 99.7%. Awaiting confirmation from one downstream consumer.",
                    PostMitigationSignalsSummary: "Overall positive signal. Error rate dropped from 12.4% to 0.3% within 20 minutes. One downstream service still reporting intermittent errors.",
                    ValidatedAt: null,
                    ValidatedBy: null);
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase)
                && workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("00000002-0001-0000-0000-000000000001"),
                    Status: ValidationStatus.Pending,
                    ExpectedChecks: new[]
                    {
                        new ValidationCheckDto("Vendor connectivity restored", "External provider should respond within timeout", false, null),
                        new ValidationCheckDto("Catalog data freshness", "Catalog data should be less than 1 hour old", false, null),
                    },
                    ObservedOutcome: null,
                    PostMitigationSignalsSummary: null,
                    ValidatedAt: null,
                    ValidatedBy: null);
            }

            return null;
        }
    }

    /// <summary>Resposta com o estado de validação pós-mitigação do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        IReadOnlyList<ValidationCheckDto> ExpectedChecks,
        string? ObservedOutcome,
        string? PostMitigationSignalsSummary,
        DateTimeOffset? ValidatedAt,
        string? ValidatedBy);

    /// <summary>Verificação individual de validação pós-mitigação.</summary>
    public sealed record ValidationCheckDto(
        string CheckName,
        string? Description,
        bool IsPassed,
        string? ObservedValue);
}
