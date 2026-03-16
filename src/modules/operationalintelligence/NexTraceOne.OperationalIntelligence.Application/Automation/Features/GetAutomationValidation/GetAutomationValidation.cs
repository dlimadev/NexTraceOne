using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;

/// <summary>
/// Feature: GetAutomationValidation — retorna o estado de validação pós-execução de um workflow,
/// incluindo verificações esperadas, resultado observado e estado geral da validação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class GetAutomationValidation
{
    /// <summary>Query para obter a validação de um workflow de automação.</summary>
    public sealed record Query(string WorkflowId) : IQuery<Response>;

    /// <summary>Valida que o identificador do workflow foi informado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe os dados de validação do workflow de automação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindValidation(request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(AutomationErrors.WorkflowNotFound(request.WorkflowId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindValidation(string workflowId)
        {
            if (workflowId.Equals("aw-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    Status: ValidationStatus.InProgress,
                    ObservedOutcome: "Error rate dropped from 5.2% to 0.4% within 15 minutes post-restart. Payment success rate recovered to 99.8%.",
                    ValidatedBy: "ops-engineer@nextraceone.io",
                    Checks: new[]
                    {
                        new ValidationCheckDto("Error rate below threshold", true, "Error rate at 0.4% — below 0.5% threshold."),
                        new ValidationCheckDto("Service response time normal", true, "P95 latency at 120ms — within acceptable range."),
                        new ValidationCheckDto("No new error patterns", true, "No new error types detected after restart."),
                        new ValidationCheckDto("Downstream consumers healthy", false, "1 of 3 downstream consumers still reporting intermittent timeouts."),
                    },
                    RecordedAt: DateTimeOffset.Parse("2024-06-15T11:00:00Z"));
            }

            if (workflowId.Equals("aw-0002-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("b0a10002-0001-0000-0000-000000000001"),
                    Status: ValidationStatus.Pending,
                    ObservedOutcome: null,
                    ValidatedBy: null,
                    Checks: new[]
                    {
                        new ValidationCheckDto("Catalog data freshness", false, null),
                        new ValidationCheckDto("Sync latency within threshold", false, null),
                    },
                    RecordedAt: null);
            }

            return null;
        }
    }

    /// <summary>Verificação individual de validação pós-execução.</summary>
    public sealed record ValidationCheckDto(
        string CheckName,
        bool IsPassed,
        string? Details);

    /// <summary>Resposta com o estado de validação pós-execução do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckDto> Checks,
        DateTimeOffset? RecordedAt);
}
