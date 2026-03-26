using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para eventos de mudança na timeline.</summary>
public interface IChangeEventRepository
{
    /// <summary>Lista eventos de uma release ordenados por data de ocorrência.</summary>
    Task<IReadOnlyList<ChangeEvent>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista eventos de uma release filtrados por tipo de evento, ordenados por data de ocorrência.
    /// Usado para consultar correlações específicas (ex: "trace_correlated").
    /// </summary>
    Task<IReadOnlyList<ChangeEvent>> ListByReleaseIdAndEventTypeAsync(ReleaseId releaseId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um evento de mudança.</summary>
    void Add(ChangeEvent changeEvent);
}
