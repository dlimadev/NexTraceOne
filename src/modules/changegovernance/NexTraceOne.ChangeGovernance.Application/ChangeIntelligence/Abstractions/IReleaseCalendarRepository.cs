using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Repositório do Release Calendar — janelas de deploy, freeze e manutenção.</summary>
public interface IReleaseCalendarRepository
{
    /// <summary>Obtém uma entrada pelo seu ID.</summary>
    Task<ReleaseCalendarEntry?> GetByIdAsync(ReleaseCalendarEntryId id, CancellationToken ct = default);

    /// <summary>Lista janelas de um tenant, opcionalmente filtradas por estado e tipo.</summary>
    Task<IReadOnlyList<ReleaseCalendarEntry>> ListAsync(
        string tenantId,
        ReleaseWindowStatus? status = null,
        ReleaseWindowType? windowType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lista janelas activas num determinado momento, opcionalmente filtradas por ambiente.
    /// Usado por IsChangeWindowOpen.
    /// </summary>
    Task<IReadOnlyList<ReleaseCalendarEntry>> ListActiveAtAsync(
        string tenantId,
        DateTimeOffset moment,
        string? environment = null,
        CancellationToken ct = default);

    /// <summary>Adiciona uma nova entrada.</summary>
    void Add(ReleaseCalendarEntry entry);

    /// <summary>Marca a entrada como modificada.</summary>
    void Update(ReleaseCalendarEntry entry);
}
