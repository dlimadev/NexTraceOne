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
}

/// <summary>DTO para uso por módulo com contagem de eventos e utilizadores únicos.</summary>
public sealed record ModuleUsageRow(ProductModule Module, long EventCount, int UniqueUsers);

/// <summary>DTO para adoção por módulo.</summary>
public sealed record ModuleAdoptionRow(ProductModule Module, long TotalActions, int UniqueUsers);

/// <summary>DTO para contagem de funcionalidades por módulo.</summary>
public sealed record ModuleFeatureCountRow(ProductModule Module, string Feature, long Count);

/// <summary>DTO para eventos de sessão individuais.</summary>
public sealed record SessionEventRow(string SessionId, AnalyticsEventType EventType, DateTimeOffset OccurredAt);
