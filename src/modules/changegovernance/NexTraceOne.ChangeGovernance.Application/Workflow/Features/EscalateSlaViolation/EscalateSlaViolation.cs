using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.EscalateSlaViolation;

/// <summary>
/// Feature: EscalateSlaViolation — escala uma violação de SLA em um estágio de workflow.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EscalateSlaViolation
{
    /// <summary>Comando para escalar uma violação de SLA.</summary>
    public sealed record Command(
        Guid WorkflowInstanceId,
        Guid StageId,
        string EscalationReason) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de escalação de SLA.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x.EscalationReason).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Handler que identifica políticas de SLA violadas e registra a escalação.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowStageRepository stageRepository,
        ISlaPolicyRepository slaPolicyRepository,
        IWorkflowUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);

            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var stage = await stageRepository.GetByIdAsync(
                WorkflowStageId.From(request.StageId), cancellationToken);

            if (stage is null)
                return WorkflowErrors.StageNotFound(request.StageId.ToString());

            var slaPolicies = await slaPolicyRepository.GetByTemplateIdAsync(
                instance.WorkflowTemplateId, cancellationToken);

            var matchingPolicy = slaPolicies.FirstOrDefault(
                p => p.StageName == stage.Name && p.EscalationEnabled);

            string? escalationTarget = matchingPolicy?.EscalationTargetRole;
            int? maxDurationHours = matchingPolicy?.MaxDurationHours;

            var escalatedPolicies = new List<EscalatedPolicyItem>();

            if (matchingPolicy is not null)
            {
                escalatedPolicies.Add(new EscalatedPolicyItem(
                    matchingPolicy.Id.Value,
                    matchingPolicy.StageName,
                    matchingPolicy.MaxDurationHours,
                    matchingPolicy.EscalationTargetRole));
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                instance.Id.Value,
                stage.Id.Value,
                request.EscalationReason,
                escalationTarget,
                maxDurationHours,
                dateTimeProvider.UtcNow,
                escalatedPolicies);
        }
    }

    /// <summary>Dados de uma política de SLA escalada.</summary>
    public sealed record EscalatedPolicyItem(
        Guid PolicyId,
        string StageName,
        int MaxDurationHours,
        string? EscalationTargetRole);

    /// <summary>Resposta da escalação de violação de SLA.</summary>
    public sealed record Response(
        Guid WorkflowInstanceId,
        Guid StageId,
        string EscalationReason,
        string? EscalationTargetRole,
        int? MaxDurationHours,
        DateTimeOffset EscalatedAt,
        IReadOnlyList<EscalatedPolicyItem> EscalatedPolicies);
}
