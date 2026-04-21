using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

/// <summary>Contrato de repositório para a entidade EvidencePack.</summary>
public interface IEvidencePackRepository
{
    /// <summary>Busca um EvidencePack pelo seu identificador.</summary>
    Task<EvidencePack?> GetByIdAsync(EvidencePackId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo EvidencePack ao repositório.</summary>
    void Add(EvidencePack evidencePack);

    /// <summary>Atualiza um EvidencePack existente.</summary>
    void Update(EvidencePack evidencePack);

    /// <summary>Busca o EvidencePack associado a uma instância de workflow.</summary>
    Task<EvidencePack?> GetByWorkflowInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default);

    /// <summary>Lista EvidencePacks associados a um conjunto de releases (batch lookup para relatórios de conformidade).</summary>
    Task<IReadOnlyList<EvidencePack>> ListByReleaseIdsAsync(IEnumerable<Guid> releaseIds, CancellationToken cancellationToken = default);
}
