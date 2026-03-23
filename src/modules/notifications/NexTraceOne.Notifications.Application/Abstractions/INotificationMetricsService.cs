namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de métricas operacionais, de consumo e de qualidade da plataforma de notificações.
/// Phase 7 — fornece visibilidade completa sobre geração, entrega, interação e ruído.
/// </summary>
public interface INotificationMetricsService
{
    /// <summary>
    /// Obtém métricas operacionais agregadas da plataforma de notificações.
    /// Inclui: total gerado, por tipo/categoria/severidade, deliveries por canal, falhas.
    /// </summary>
    Task<NotificationPlatformMetrics> GetPlatformMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém métricas de interação do utilizador com as notificações.
    /// Inclui: taxa de leitura, tempo até leitura, taxa de acknowledge, snooze, etc.
    /// </summary>
    Task<NotificationInteractionMetrics> GetInteractionMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém métricas de qualidade e ruído da plataforma.
    /// Inclui: notificações por utilizador, taxa de duplicação, suppressão, grouping, etc.
    /// </summary>
    Task<NotificationQualityMetrics> GetQualityMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Métricas operacionais da plataforma de notificações.
/// </summary>
public sealed record NotificationPlatformMetrics
{
    /// <summary>Total de notificações geradas no período.</summary>
    public int TotalGenerated { get; init; }

    /// <summary>Contagem por categoria.</summary>
    public IReadOnlyDictionary<string, int> ByCategory { get; init; } = new Dictionary<string, int>();

    /// <summary>Contagem por severidade.</summary>
    public IReadOnlyDictionary<string, int> BySeverity { get; init; } = new Dictionary<string, int>();

    /// <summary>Contagem por módulo de origem.</summary>
    public IReadOnlyDictionary<string, int> BySourceModule { get; init; } = new Dictionary<string, int>();

    /// <summary>Total de deliveries por canal.</summary>
    public IReadOnlyDictionary<string, int> DeliveriesByChannel { get; init; } = new Dictionary<string, int>();

    /// <summary>Total de deliveries com sucesso.</summary>
    public int TotalDelivered { get; init; }

    /// <summary>Total de deliveries falhadas.</summary>
    public int TotalFailed { get; init; }

    /// <summary>Total de deliveries pendentes.</summary>
    public int TotalPending { get; init; }

    /// <summary>Total de deliveries ignoradas (skipped).</summary>
    public int TotalSkipped { get; init; }
}

/// <summary>
/// Métricas de interação e consumo do utilizador.
/// </summary>
public sealed record NotificationInteractionMetrics
{
    /// <summary>Total de notificações lidas no período.</summary>
    public int TotalRead { get; init; }

    /// <summary>Total de notificações não lidas.</summary>
    public int TotalUnread { get; init; }

    /// <summary>Total de notificações acknowledged.</summary>
    public int TotalAcknowledged { get; init; }

    /// <summary>Total de notificações snoozed.</summary>
    public int TotalSnoozed { get; init; }

    /// <summary>Total de notificações arquivadas.</summary>
    public int TotalArchived { get; init; }

    /// <summary>Total de notificações descartadas.</summary>
    public int TotalDismissed { get; init; }

    /// <summary>Total de notificações escaladas.</summary>
    public int TotalEscalated { get; init; }

    /// <summary>Taxa de leitura (read / total gerado).</summary>
    public decimal ReadRate { get; init; }

    /// <summary>Taxa de acknowledge (acknowledged / total que requer ação).</summary>
    public decimal AcknowledgeRate { get; init; }
}

/// <summary>
/// Métricas de qualidade e ruído da plataforma.
/// </summary>
public sealed record NotificationQualityMetrics
{
    /// <summary>Média de notificações por utilizador por dia.</summary>
    public decimal AveragePerUserPerDay { get; init; }

    /// <summary>Total de notificações suprimidas.</summary>
    public int TotalSuppressed { get; init; }

    /// <summary>Total de notificações agrupadas (com GroupId).</summary>
    public int TotalGrouped { get; init; }

    /// <summary>Total de notificações correlacionadas com incidentes.</summary>
    public int TotalCorrelatedWithIncidents { get; init; }

    /// <summary>Tipos de notificação que mais geram volume (top 5).</summary>
    public IReadOnlyList<NotificationTypeCount> TopNoisyTypes { get; init; } = [];

    /// <summary>Tipos de notificação com menor taxa de leitura (top 5).</summary>
    public IReadOnlyList<NotificationTypeCount> LeastEngagedTypes { get; init; } = [];
}

/// <summary>
/// Contagem de um tipo de notificação para métricas.
/// </summary>
public sealed record NotificationTypeCount(string EventType, int Count);
