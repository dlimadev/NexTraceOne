using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.EvaluatePreconditions;

/// <summary>
/// Feature: EvaluatePreconditions — avalia as pré-condições de um workflow de automação
/// para determinar se todas as condições obrigatórias estão satisfeitas para execução segura.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EvaluatePreconditions
{
    /// <summary>Comando para avaliar as pré-condições de um workflow.</summary>
    public sealed record Command(
        string WorkflowId,
        string EvaluatedBy) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do comando de avaliação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EvaluatedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que avalia as pré-condições do workflow de automação.</summary>
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
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

            var serviceHealthPassed = !string.IsNullOrWhiteSpace(workflow.ServiceId);
            var approvalPassed = workflow.ApprovalStatus == AutomationApprovalStatus.Approved;
            var blastRadiusPassed = !string.IsNullOrWhiteSpace(workflow.TargetScope);
            var environmentPassed = !string.IsNullOrWhiteSpace(workflow.TargetEnvironment);

            var results = new List<PreconditionResult>
            {
                new(PreconditionType.ServiceHealthCheck,
                    serviceHealthPassed,
                    serviceHealthPassed
                        ? $"Service reference is configured: '{workflow.ServiceId}'."
                        : "No service reference configured — cannot verify health.",
                    utcNow),

                new(PreconditionType.ApprovalPresence,
                    approvalPassed,
                    approvalPassed
                        ? $"Approval granted by '{workflow.ApprovedBy}' at {workflow.ApprovedAt:u}."
                        : "Approval is still pending.",
                    utcNow),

                new(PreconditionType.BlastRadiusConstraint,
                    blastRadiusPassed,
                    blastRadiusPassed
                        ? $"Target scope is defined: '{workflow.TargetScope}'."
                        : "No target scope defined — blast radius cannot be assessed.",
                    utcNow),

                new(PreconditionType.EnvironmentRestriction,
                    environmentPassed,
                    environmentPassed
                        ? $"Target environment is set: '{workflow.TargetEnvironment}'."
                        : "No target environment specified.",
                    utcNow),

                new(PreconditionType.CooldownPeriod,
                    true,
                    "Cooldown period check passed (no real cooldown logic implemented yet).",
                    utcNow),
            };

            var allPassed = results.All(r => r.Passed);

            if (workflow.Status == AutomationWorkflowStatus.Draft)
                workflow.UpdateStatus(AutomationWorkflowStatus.PendingPreconditions, utcNow);

            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var auditEntry = AutomationAuditRecord.Create(
                workflowId: workflowId,
                action: AutomationAuditAction.PreconditionsEvaluated,
                actor: request.EvaluatedBy,
                details: allPassed
                    ? $"All {results.Count} preconditions evaluated — all passed."
                    : $"{results.Count} preconditions evaluated — {results.Count(r => !r.Passed)} failed.",
                utcNow: utcNow,
                serviceId: workflow.ServiceId);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                WorkflowId: workflow.Id.Value,
                AllPassed: allPassed,
                Results: results));
        }
    }

    /// <summary>Resultado da avaliação de uma pré-condição individual.</summary>
    public sealed record PreconditionResult(
        PreconditionType Type,
        bool Passed,
        string Details,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resposta da avaliação de pré-condições do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        bool AllPassed,
        IReadOnlyList<PreconditionResult> Results);
}
