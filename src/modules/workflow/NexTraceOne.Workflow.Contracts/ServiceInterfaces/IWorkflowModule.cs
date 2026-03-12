namespace NexTraceOne.Workflow.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Workflow.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IWorkflowModule
{
    /// <summary>Obtém o status atual de um workflow por ID da instância.</summary>
    Task<WorkflowStatusDto?> GetWorkflowStatusAsync(Guid workflowInstanceId, CancellationToken cancellationToken);

    /// <summary>Verifica se uma release possui workflow aprovado.</summary>
    Task<bool> IsReleaseApprovedAsync(Guid releaseId, CancellationToken cancellationToken);

    /// <summary>Obtém o evidence pack de uma instância de workflow.</summary>
    Task<EvidencePackDto?> GetEvidencePackAsync(Guid workflowInstanceId, CancellationToken cancellationToken);
}

/// <summary>DTO de status de workflow para comunicação entre módulos.</summary>
public sealed record WorkflowStatusDto(
    Guid WorkflowInstanceId,
    Guid ReleaseId,
    string Status,
    string SubmittedBy,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? CompletedAt);

/// <summary>DTO de evidence pack para comunicação entre módulos.</summary>
public sealed record EvidencePackDto(
    Guid EvidencePackId,
    Guid WorkflowInstanceId,
    decimal? BlastRadiusScore,
    decimal? SpectralScore,
    decimal? ChangeIntelligenceScore,
    decimal CompletenessPercentage,
    DateTimeOffset GeneratedAt);
