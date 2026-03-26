using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.ExportEvidencePackPdf;

/// <summary>
/// Feature: ExportEvidencePackPdf — exporta os dados estruturados do evidence pack para geração futura de PDF.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExportEvidencePackPdf
{
    /// <summary>Query para exportar os dados do evidence pack em formato estruturado.</summary>
    public sealed record Query(Guid WorkflowInstanceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de exportação de evidence pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
        }
    }

    /// <summary>Handler que busca o evidence pack e as decisões de aprovação para montar o relatório.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IWorkflowStageRepository stageRepository,
        IApprovalDecisionRepository decisionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);

            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var evidencePack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(
                instanceId, cancellationToken);

            if (evidencePack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            var stages = await stageRepository.ListByInstanceIdAsync(instanceId, cancellationToken);
            var decisions = await decisionRepository.ListByInstanceIdAsync(instanceId, cancellationToken);

            var stageDetails = stages
                .Select(s => new StageDetail(
                    s.Id.Value,
                    s.Name,
                    s.StageOrder,
                    s.Status.ToString(),
                    s.RequiredApprovers,
                    s.CurrentApprovals,
                    s.StartedAt,
                    s.CompletedAt))
                .ToList();

            var decisionDetails = decisions
                .Select(d => new DecisionDetail(
                    d.Id.Value,
                    d.WorkflowStageId.Value,
                    d.DecidedBy,
                    d.Decision.ToString(),
                    d.Comment,
                    d.DecidedAt))
                .ToList();

            return new Response(
                evidencePack.Id.Value,
                instance.Id.Value,
                instance.ReleaseId,
                instance.Status.ToString(),
                instance.SubmittedBy,
                instance.SubmittedAt,
                instance.CompletedAt,
                evidencePack.ContractDiffSummary,
                evidencePack.BlastRadiusScore,
                evidencePack.SpectralScore,
                evidencePack.ChangeIntelligenceScore,
                evidencePack.ContractHash,
                evidencePack.CompletenessPercentage,
                evidencePack.GeneratedAt,
                stageDetails,
                decisionDetails,
                evidencePack.PipelineSource,
                evidencePack.BuildId,
                evidencePack.CommitSha,
                evidencePack.CiChecksResult);
        }
    }

    /// <summary>Dados de um estágio para o relatório de exportação.</summary>
    public sealed record StageDetail(
        Guid StageId,
        string Name,
        int StageOrder,
        string Status,
        int RequiredApprovers,
        int CurrentApprovals,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);

    /// <summary>Dados de uma decisão para o relatório de exportação.</summary>
    public sealed record DecisionDetail(
        Guid DecisionId,
        Guid StageId,
        string DecidedBy,
        string Decision,
        string? Comment,
        DateTimeOffset DecidedAt);

    /// <summary>Resposta estruturada com todos os dados do evidence pack para geração de PDF.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        Guid WorkflowInstanceId,
        Guid ReleaseId,
        string WorkflowStatus,
        string SubmittedBy,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? CompletedAt,
        string? ContractDiffSummary,
        decimal? BlastRadiusScore,
        decimal? SpectralScore,
        decimal? ChangeIntelligenceScore,
        string? ContractHash,
        decimal CompletenessPercentage,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<StageDetail> Stages,
        IReadOnlyList<DecisionDetail> Decisions,
        string? PipelineSource,
        string? BuildId,
        string? CommitSha,
        string? CiChecksResult);
}
