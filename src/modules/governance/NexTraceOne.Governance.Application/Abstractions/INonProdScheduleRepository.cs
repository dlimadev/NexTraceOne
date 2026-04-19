using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Contrato de repositório para agendas de ambientes não produtivos.</summary>
public interface INonProdScheduleRepository
{
    /// <summary>Lista todas as agendas activas.</summary>
    Task<IReadOnlyList<NonProdSchedule>> ListAllAsync(CancellationToken ct);

    /// <summary>Busca uma agenda pelo identificador de negócio do ambiente.</summary>
    Task<NonProdSchedule?> GetByEnvironmentIdAsync(string environmentId, CancellationToken ct);

    /// <summary>Adiciona uma nova agenda.</summary>
    Task AddAsync(NonProdSchedule schedule, CancellationToken ct);

    /// <summary>Atualiza uma agenda existente.</summary>
    void Update(NonProdSchedule schedule);
}
