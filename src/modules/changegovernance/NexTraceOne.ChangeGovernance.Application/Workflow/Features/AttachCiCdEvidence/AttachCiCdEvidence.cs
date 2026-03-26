using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.AttachCiCdEvidence;

/// <summary>
/// Feature: AttachCiCdEvidence — anexa evidências automáticas de pipeline CI/CD ao EvidencePack
/// de uma instância de workflow, usando os sinais já registados via ExternalMarker no deploy.
///
/// Este é o ponto de entrada automático que liga CI/CD → EvidencePack.
/// Chamado quando um evento de pipeline (DeploymentStarted/DeploymentCompleted) é recebido.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AttachCiCdEvidence
{
    /// <summary>
    /// Comando para anexar evidências de CI/CD ao EvidencePack de uma instância de workflow.
    /// Os dados devem vir do ExternalMarker criado pelo NotifyDeployment (P5.1).
    /// </summary>
    public sealed record Command(
        Guid WorkflowInstanceId,
        string PipelineSource,
        string? BuildId,
        string? CommitSha,
        string? CiChecksResult) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de anexação de evidências CI/CD.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
            RuleFor(x => x.PipelineSource).NotEmpty().MaximumLength(500);
            RuleFor(x => x.BuildId).MaximumLength(500).When(x => x.BuildId is not null);
            RuleFor(x => x.CommitSha).MaximumLength(100).When(x => x.CommitSha is not null);
            RuleFor(x => x.CiChecksResult)
                .Must(v => v is null or "passed" or "failed" or "partial" or "unknown")
                .WithMessage("CiChecksResult must be one of: passed, failed, partial, unknown");
        }
    }

    /// <summary>Handler que localiza o EvidencePack e anexa as evidências de CI/CD.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);

            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var pack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(instanceId, cancellationToken);
            if (pack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            pack.AttachCiCdEvidence(
                request.PipelineSource,
                request.BuildId,
                request.CommitSha,
                request.CiChecksResult);

            evidencePackRepository.Update(pack);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                pack.Id.Value,
                pack.WorkflowInstanceId.Value,
                pack.PipelineSource!,
                pack.BuildId,
                pack.CommitSha,
                pack.CiChecksResult,
                pack.CompletenessPercentage);
        }
    }

    /// <summary>Resposta da anexação de evidências CI/CD ao EvidencePack.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        Guid WorkflowInstanceId,
        string PipelineSource,
        string? BuildId,
        string? CommitSha,
        string? CiChecksResult,
        decimal CompletenessPercentage);
}
