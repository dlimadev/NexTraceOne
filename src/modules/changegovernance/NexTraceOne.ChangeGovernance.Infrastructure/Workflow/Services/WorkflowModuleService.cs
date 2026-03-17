using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.Workflow.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Services;

/// <summary>
/// Implementação concreta da interface pública do módulo Workflow.
/// Expõe dados do workflow para outros módulos sem expor repositórios internos.
/// </summary>
public sealed class WorkflowModuleService(
    IWorkflowInstanceRepository instanceRepository,
    IEvidencePackRepository evidencePackRepository) : IWorkflowModule
{
    /// <summary>
    /// Obtém o status atual de uma instância de workflow pelo seu identificador.
    /// Retorna null se a instância não existir.
    /// </summary>
    public async Task<WorkflowStatusDto?> GetWorkflowStatusAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        var instance = await instanceRepository.GetByIdAsync(
            WorkflowInstanceId.From(workflowInstanceId), cancellationToken);

        if (instance is null)
            return null;

        return new WorkflowStatusDto(
            instance.Id.Value,
            instance.ReleaseId,
            instance.Status.ToString(),
            instance.SubmittedBy,
            instance.SubmittedAt,
            instance.CompletedAt);
    }

    /// <summary>
    /// Verifica se uma release possui workflow aprovado.
    /// Retorna false se não houver instância de workflow associada à release.
    /// </summary>
    public async Task<bool> IsReleaseApprovedAsync(
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var instance = await instanceRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

        return instance is not null && instance.Status == WorkflowStatus.Approved;
    }

    /// <summary>
    /// Obtém o evidence pack associado a uma instância de workflow.
    /// Retorna null se não existir evidence pack para a instância.
    /// </summary>
    public async Task<EvidencePackDto?> GetEvidencePackAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        var evidencePack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(
            WorkflowInstanceId.From(workflowInstanceId), cancellationToken);

        if (evidencePack is null)
            return null;

        return new EvidencePackDto(
            evidencePack.Id.Value,
            evidencePack.WorkflowInstanceId.Value,
            evidencePack.BlastRadiusScore,
            evidencePack.SpectralScore,
            evidencePack.ChangeIntelligenceScore,
            evidencePack.CompletenessPercentage,
            evidencePack.GeneratedAt);
    }
}
