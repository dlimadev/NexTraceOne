using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.RecordAutomationValidation;

/// <summary>
/// Feature: RecordAutomationValidation — regista o resultado de uma validação pós-execução
/// de um workflow de automação, incluindo verificações individuais e resultado observado.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordAutomationValidation
{
    /// <summary>Comando para registar a validação pós-execução de um workflow.</summary>
    public sealed record Command(
        string WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckInput>? Checks) : ICommand<Response>;

    /// <summary>Entrada de verificação individual para registo de validação.</summary>
    public sealed record ValidationCheckInput(
        string CheckName,
        bool Passed,
        string? Details);

    /// <summary>Valida os campos obrigatórios do comando de registo de validação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.ObservedOutcome).MaximumLength(2000).When(x => x.ObservedOutcome is not null);
            RuleFor(x => x.ValidatedBy).MaximumLength(200).When(x => x.ValidatedBy is not null);
        }
    }

    /// <summary>Handler que regista a validação do workflow de automação.</summary>
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
        IAutomationValidationRepository validationRepository,
        IAutomationAuditRepository auditRepository,
        IAutomationUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedId))
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var workflowId = new AutomationWorkflowRecordId(parsedId);
            var workflow = await workflowRepository.GetByIdAsync(workflowId, cancellationToken);

            if (workflow is null)
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var utcNow = clock.UtcNow;
            var outcome = MapValidationStatusToOutcome(request.Status);

            var notes = request.Checks is { Count: > 0 }
                ? string.Join("; ", request.Checks.Select(c => $"{c.CheckName}: {(c.Passed ? "Passed" : "Failed")}{(c.Details is not null ? $" — {c.Details}" : "")}"))
                : string.Empty;

            var validation = AutomationValidationRecord.Create(
                workflowId: workflowId,
                outcome: outcome,
                validatedBy: request.ValidatedBy ?? "system",
                notes: notes,
                observedOutcome: request.ObservedOutcome,
                utcNow: utcNow);

            await validationRepository.AddAsync(validation, cancellationToken);

            var newStatus = request.Status switch
            {
                ValidationStatus.Passed => AutomationWorkflowStatus.Completed,
                ValidationStatus.Failed => AutomationWorkflowStatus.Failed,
                _ => AutomationWorkflowStatus.AwaitingValidation
            };

            workflow.UpdateStatus(newStatus, utcNow);
            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var auditEntry = AutomationAuditRecord.Create(
                workflowId: workflowId,
                action: AutomationAuditAction.ValidationRecorded,
                actor: request.ValidatedBy ?? "system",
                details: $"Validation recorded with outcome '{outcome}'. Observed: {request.ObservedOutcome ?? "N/A"}",
                utcNow: utcNow,
                serviceId: workflow.ServiceId);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                WorkflowId: workflow.Id.Value,
                ValidationStatus: request.Status,
                RecordedAt: utcNow));
        }

        private static AutomationOutcome MapValidationStatusToOutcome(ValidationStatus status) => status switch
        {
            ValidationStatus.Passed => AutomationOutcome.Successful,
            ValidationStatus.Failed => AutomationOutcome.Failed,
            _ => AutomationOutcome.Inconclusive
        };
    }

    /// <summary>Resposta do registo de validação do workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus ValidationStatus,
        DateTimeOffset RecordedAt);
}
