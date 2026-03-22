using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.CreateAutomationWorkflow;

/// <summary>
/// Feature: CreateAutomationWorkflow — cria um novo workflow de automação operacional,
/// associando uma ação do catálogo a um serviço, incidente ou alteração com rastreabilidade completa.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateAutomationWorkflow
{
    /// <summary>Comando para criar um workflow de automação.</summary>
    public sealed record Command(
        string ActionId,
        string? ServiceId,
        string? IncidentId,
        string? ChangeId,
        string Rationale,
        string RequestedBy,
        string? TargetScope,
        string? TargetEnvironment) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do comando de criação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ActionId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Rationale).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.IncidentId).MaximumLength(200).When(x => x.IncidentId is not null);
            RuleFor(x => x.ChangeId).MaximumLength(200).When(x => x.ChangeId is not null);
            RuleFor(x => x.TargetScope).MaximumLength(200).When(x => x.TargetScope is not null);
            RuleFor(x => x.TargetEnvironment).MaximumLength(200).When(x => x.TargetEnvironment is not null);
        }
    }

    /// <summary>Handler que cria e persiste o workflow de automação.</summary>
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
        IAutomationAuditRepository auditRepository,
        IAutomationUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = AutomationActionCatalog.GetAll();
            var action = catalog.FirstOrDefault(a =>
                a.ActionId.Equals(request.ActionId, StringComparison.OrdinalIgnoreCase));

            if (action is null)
                return AutomationErrors.ActionNotFound(request.ActionId);

            var utcNow = clock.UtcNow;

            var workflow = AutomationWorkflowRecord.Create(
                actionId: request.ActionId,
                serviceId: request.ServiceId,
                incidentId: request.IncidentId,
                changeId: request.ChangeId,
                rationale: request.Rationale,
                requestedBy: request.RequestedBy,
                targetScope: request.TargetScope,
                targetEnvironment: request.TargetEnvironment,
                riskLevel: action.RiskLevel,
                utcNow: utcNow);

            await workflowRepository.AddAsync(workflow, cancellationToken);

            var auditEntry = AutomationAuditRecord.Create(
                workflowId: workflow.Id,
                action: AutomationAuditAction.WorkflowCreated,
                actor: request.RequestedBy,
                details: $"Workflow created for action '{action.DisplayName}'. Rationale: {request.Rationale}",
                utcNow: utcNow,
                serviceId: request.ServiceId);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(
                WorkflowId: workflow.Id.Value,
                Status: AutomationWorkflowStatus.Draft,
                ActionId: request.ActionId,
                CreatedAt: utcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta da criação do workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        AutomationWorkflowStatus Status,
        string ActionId,
        DateTimeOffset CreatedAt);
}
