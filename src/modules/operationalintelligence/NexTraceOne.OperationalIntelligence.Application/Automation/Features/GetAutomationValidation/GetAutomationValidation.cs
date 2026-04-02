using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;

/// <summary>
/// Feature: GetAutomationValidation — retorna o estado de validação pós-execução de um workflow,
/// incluindo verificações esperadas, resultado observado e estado geral da validação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
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
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
        IAutomationValidationRepository validationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedId))
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var workflowId = new AutomationWorkflowRecordId(parsedId);
            var workflow = await workflowRepository.GetByIdAsync(workflowId, cancellationToken);

            if (workflow is null)
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var validationRecord = await validationRepository.GetByWorkflowIdAsync(workflowId, cancellationToken);

            if (validationRecord is null)
            {
                return Result<Response>.Success(new Response(
                    WorkflowId: workflow.Id.Value,
                    Status: ValidationStatus.Pending,
                    ObservedOutcome: null,
                    ValidatedBy: null,
                    Checks: Array.Empty<ValidationCheckDto>(),
                    RecordedAt: null));
            }

            var response = new Response(
                WorkflowId: workflow.Id.Value,
                Status: MapOutcomeToValidationStatus(validationRecord.Outcome),
                ObservedOutcome: validationRecord.ObservedOutcome,
                ValidatedBy: validationRecord.ValidatedBy,
                Checks: Array.Empty<ValidationCheckDto>(),
                RecordedAt: validationRecord.ValidatedAt);

            return Result<Response>.Success(response);
        }

        private static ValidationStatus MapOutcomeToValidationStatus(AutomationOutcome outcome) => outcome switch
        {
            AutomationOutcome.Successful => ValidationStatus.Passed,
            AutomationOutcome.Failed => ValidationStatus.Failed,
            AutomationOutcome.Cancelled => ValidationStatus.Failed,
            _ => ValidationStatus.InProgress
        };
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
