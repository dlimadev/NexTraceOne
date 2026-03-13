using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.GetEvidencePack;

/// <summary>
/// Feature: GetEvidencePack — retorna o evidence pack associado a uma instância de workflow.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetEvidencePack
{
    /// <summary>Query para obter o evidence pack de uma instância de workflow.</summary>
    public sealed record Query(Guid WorkflowInstanceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de evidence pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
        }
    }

    /// <summary>Handler que busca o evidence pack pela instância de workflow.</summary>
    public sealed class Handler(
        IEvidencePackRepository evidencePackRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var evidencePack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(
                WorkflowInstanceId.From(request.WorkflowInstanceId), cancellationToken);

            if (evidencePack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            return new Response(
                evidencePack.Id.Value,
                evidencePack.WorkflowInstanceId.Value,
                evidencePack.ReleaseId,
                evidencePack.ContractDiffSummary,
                evidencePack.BlastRadiusScore,
                evidencePack.SpectralScore,
                evidencePack.ChangeIntelligenceScore,
                evidencePack.ApprovalHistory,
                evidencePack.ContractHash,
                evidencePack.CompletenessPercentage,
                evidencePack.GeneratedAt);
        }
    }

    /// <summary>Resposta com os dados completos do evidence pack.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        Guid WorkflowInstanceId,
        Guid ReleaseId,
        string? ContractDiffSummary,
        decimal? BlastRadiusScore,
        decimal? SpectralScore,
        decimal? ChangeIntelligenceScore,
        string? ApprovalHistory,
        string? ContractHash,
        decimal CompletenessPercentage,
        DateTimeOffset GeneratedAt);
}
