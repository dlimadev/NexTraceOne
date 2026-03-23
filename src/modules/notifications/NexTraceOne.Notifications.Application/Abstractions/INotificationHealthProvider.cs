namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Provider de health da plataforma de notificações.
/// Phase 7 — permite diagnosticar o estado operacional do módulo.
/// </summary>
public interface INotificationHealthProvider
{
    /// <summary>
    /// Obtém o estado de saúde actual da plataforma de notificações.
    /// Verifica: canais, orchestrator, store, jobs de digest/escalation.
    /// </summary>
    Task<NotificationHealthReport> GetHealthAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Relatório de saúde da plataforma de notificações.
/// </summary>
public sealed record NotificationHealthReport
{
    /// <summary>Estado geral da plataforma.</summary>
    public required NotificationHealthStatus OverallStatus { get; init; }

    /// <summary>Estado de cada componente verificado.</summary>
    public required IReadOnlyList<NotificationComponentHealth> Components { get; init; }

    /// <summary>Data/hora do relatório.</summary>
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Estado de saúde de um componente da plataforma.
/// </summary>
public sealed record NotificationComponentHealth
{
    /// <summary>Nome do componente (e.g., "InAppStore", "EmailChannel", "TeamsChannel").</summary>
    public required string Name { get; init; }

    /// <summary>Estado do componente.</summary>
    public required NotificationHealthStatus Status { get; init; }

    /// <summary>Descrição do estado ou mensagem de erro.</summary>
    public string? Description { get; init; }

    /// <summary>Métricas adicionais do componente.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Estados de saúde da plataforma de notificações.
/// </summary>
public enum NotificationHealthStatus
{
    /// <summary>Componente operacional e saudável.</summary>
    Healthy = 0,

    /// <summary>Componente operacional com degradação.</summary>
    Degraded = 1,

    /// <summary>Componente indisponível ou falhando.</summary>
    Unhealthy = 2
}
