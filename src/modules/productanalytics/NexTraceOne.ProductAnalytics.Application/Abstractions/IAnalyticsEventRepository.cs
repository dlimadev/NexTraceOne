using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Abstractions;

/// <summary>
/// Interface do repositório de AnalyticsEvents para o módulo Product Analytics.
/// Define operações de escrita e consultas analíticas agregadas.
/// </summary>
public interface IAnalyticsEventRepository
{
    Task AddAsync(AnalyticsEvent analyticsEvent, CancellationToken ct);

    Task<long> CountAsync(
        string? persona,
        ProductModule? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Conta eventos filtrados por tipo de evento (ex: ZeroResultSearch, JourneyAbandoned).</summary>
    Task<long> CountByEventTypeAsync(
        AnalyticsEventType eventType,
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<int> CountUniqueUsersAsync(
        string? persona,
        ProductModule? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<int> CountActivePersonasAsync(
        string? module,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<ModuleUsageRow>> GetTopModulesAsync(
        string? persona,
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        int top,
        CancellationToken ct);

    Task<IReadOnlyList<ModuleAdoptionRow>> GetModuleAdoptionAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<ModuleFeatureCountRow>> GetFeatureCountsAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<SessionEventRow>> ListSessionEventsAsync(
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Agrupa eventos por persona e retorna contagem de eventos e utilizadores únicos.</summary>
    Task<IReadOnlyList<PersonaBreakdownRow>> GetPersonaBreakdownAsync(
        string? teamId,
        string? domainId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Retorna tipos de evento mais frequentes opcionalmente filtrados por persona.</summary>
    Task<IReadOnlyList<EventTypeCountRow>> GetTopEventTypesAsync(
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        int top,
        CancellationToken ct);

    /// <summary>Retorna tipos de evento distintos para uma persona no período.</summary>
    Task<IReadOnlyList<AnalyticsEventType>> GetDistinctEventTypesAsync(
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Conta utilizadores únicos por tipo de evento para um conjunto de tipos.</summary>
    Task<IReadOnlyList<EventTypeUserCountRow>> CountUsersByEventTypeAsync(
        AnalyticsEventType[] eventTypes,
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Retorna presença de tipos de evento por sessão — para computação de funil.</summary>
    Task<IReadOnlyList<SessionEventTypeRow>> GetSessionEventTypesAsync(
        AnalyticsEventType[] eventTypes,
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Conta sessões distintas no período.</summary>
    Task<int> CountDistinctSessionsAsync(
        string? persona,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Retorna tempo médio desde o primeiro evento do utilizador até o primeiro evento de cada tipo.</summary>
    Task<IReadOnlyList<UserFirstEventRow>> GetUserFirstEventTimesAsync(
        AnalyticsEventType[] eventTypes,
        string? persona,
        string? teamId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

/// <summary>DTO para uso por módulo com contagem de eventos e utilizadores únicos.</summary>
public sealed record ModuleUsageRow(ProductModule Module, long EventCount, int UniqueUsers);

/// <summary>DTO para adoção por módulo.</summary>
public sealed record ModuleAdoptionRow(ProductModule Module, long TotalActions, int UniqueUsers);

/// <summary>DTO para contagem de funcionalidades por módulo.</summary>
public sealed record ModuleFeatureCountRow(ProductModule Module, string Feature, long Count);

/// <summary>DTO para eventos de sessão individuais.</summary>
public sealed record SessionEventRow(string SessionId, AnalyticsEventType EventType, DateTimeOffset OccurredAt);

/// <summary>DTO para breakdown por persona: contagem de eventos e utilizadores únicos.</summary>
public sealed record PersonaBreakdownRow(string Persona, long EventCount, int UniqueUsers);

/// <summary>DTO para contagem de eventos por tipo.</summary>
public sealed record EventTypeCountRow(AnalyticsEventType EventType, long Count);

/// <summary>DTO para contagem de utilizadores únicos por tipo de evento.</summary>
public sealed record EventTypeUserCountRow(AnalyticsEventType EventType, int UniqueUsers);

/// <summary>DTO para presença de tipo de evento por sessão com timestamp da primeira ocorrência.</summary>
public sealed record SessionEventTypeRow(string SessionId, AnalyticsEventType EventType, DateTimeOffset FirstOccurrence);

/// <summary>DTO para primeiro evento de um utilizador por tipo — usado para cálculo de time-to-milestone.</summary>
public sealed record UserFirstEventRow(string UserId, AnalyticsEventType EventType, DateTimeOffset FirstOccurrence);
