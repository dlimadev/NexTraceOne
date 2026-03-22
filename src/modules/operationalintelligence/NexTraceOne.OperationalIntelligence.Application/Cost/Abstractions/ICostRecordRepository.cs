using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade CostRecord.
/// Provê operações de leitura e escrita para registos individuais de custo.
/// </summary>
public interface ICostRecordRepository
{
    /// <summary>Busca um registo de custo pelo seu identificador.</summary>
    Task<CostRecord?> GetByIdAsync(CostRecordId id, CancellationToken cancellationToken = default);

    /// <summary>Lista registos de custo por período, ordenados por custo total descendente.</summary>
    Task<IReadOnlyList<CostRecord>> ListByPeriodAsync(string period, CancellationToken cancellationToken = default);

    /// <summary>Lista registos de custo de um serviço específico, opcionalmente filtrado por período.</summary>
    Task<IReadOnlyList<CostRecord>> ListByServiceAsync(string serviceId, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>Lista registos de custo de uma equipa, opcionalmente filtrado por período.</summary>
    Task<IReadOnlyList<CostRecord>> ListByTeamAsync(string team, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>Lista registos de custo de um domínio, opcionalmente filtrado por período.</summary>
    Task<IReadOnlyList<CostRecord>> ListByDomainAsync(string domain, string? period = null, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo registo de custo ao repositório.</summary>
    void Add(CostRecord record);

    /// <summary>Adiciona múltiplos registos de custo ao repositório em batch.</summary>
    void AddRange(IEnumerable<CostRecord> records);
}
