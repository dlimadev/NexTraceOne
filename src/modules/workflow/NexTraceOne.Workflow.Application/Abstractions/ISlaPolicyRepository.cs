using NexTraceOne.Workflow.Domain.Entities;

namespace NexTraceOne.Workflow.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade SlaPolicy.</summary>
public interface ISlaPolicyRepository
{
    /// <summary>Busca uma SlaPolicy pelo seu identificador.</summary>
    Task<SlaPolicy?> GetByIdAsync(SlaPolicyId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova SlaPolicy ao repositório.</summary>
    void Add(SlaPolicy policy);

    /// <summary>Atualiza uma SlaPolicy existente.</summary>
    void Update(SlaPolicy policy);

    /// <summary>Lista políticas de SLA de um template de workflow.</summary>
    Task<IReadOnlyList<SlaPolicy>> GetByTemplateIdAsync(WorkflowTemplateId templateId, CancellationToken cancellationToken = default);

    /// <summary>Lista políticas de SLA com violações expiradas (estágios que ultrapassaram o tempo máximo).</summary>
    Task<IReadOnlyList<SlaPolicy>> ListExpiredAsync(CancellationToken cancellationToken = default);
}
