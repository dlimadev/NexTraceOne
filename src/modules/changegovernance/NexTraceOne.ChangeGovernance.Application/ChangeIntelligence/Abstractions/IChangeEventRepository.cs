using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Application.Abstractions;

/// <summary>Contrato de repositório para eventos de mudança na timeline.</summary>
public interface IChangeEventRepository
{
    /// <summary>Lista eventos de uma release ordenados por data de ocorrência.</summary>
    Task<IReadOnlyList<ChangeEvent>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um evento de mudança.</summary>
    void Add(ChangeEvent changeEvent);
}
