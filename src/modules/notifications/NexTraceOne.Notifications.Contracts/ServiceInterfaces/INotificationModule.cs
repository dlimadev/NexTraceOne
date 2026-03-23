namespace NexTraceOne.Notifications.Contracts.ServiceInterfaces;

/// <summary>
/// Contrato público do módulo de notificações.
/// Permite que outros módulos enviem notificações sem acoplamento direto à implementação.
/// </summary>
public interface INotificationModule
{
    /// <summary>
    /// Submete um pedido de notificação para processamento pelo orquestrador.
    /// O orquestrador decidirá: se notifica, quem, por qual canal, com qual template.
    /// </summary>
    Task<NotificationResult> SubmitAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a contagem de notificações não lidas para um utilizador.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Pedido de notificação submetido por módulos produtores.
/// Contém todo o contexto necessário para o orquestrador decidir roteamento e canais.
/// </summary>
public sealed record NotificationRequest
{
    /// <summary>Tipo do evento que origina a notificação (e.g., "IncidentCreated").</summary>
    public required string EventType { get; init; }

    /// <summary>Categoria da notificação (mapeia para NotificationCategory).</summary>
    public required string Category { get; init; }

    /// <summary>Severidade da notificação (mapeia para NotificationSeverity).</summary>
    public required string Severity { get; init; }

    /// <summary>Título curto da notificação, adequado para exibição em lista e push.</summary>
    public required string Title { get; init; }

    /// <summary>Mensagem completa com contexto de negócio.</summary>
    public required string Message { get; init; }

    /// <summary>Módulo de origem que produziu o evento.</summary>
    public required string SourceModule { get; init; }

    /// <summary>Tipo da entidade de origem (e.g., "Incident", "Release", "Contract").</summary>
    public string? SourceEntityType { get; init; }

    /// <summary>Id da entidade de origem para deep linking.</summary>
    public string? SourceEntityId { get; init; }

    /// <summary>URL de ação / deep link para navegação direta ao contexto.</summary>
    public string? ActionUrl { get; init; }

    /// <summary>Se true, a notificação exige ação ou acknowledge do destinatário.</summary>
    public bool RequiresAction { get; init; }

    /// <summary>Id do tenant (obrigatório em contexto multi-tenant).</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Id do ambiente onde o evento ocorreu.</summary>
    public Guid? EnvironmentId { get; init; }

    /// <summary>Ids dos utilizadores destinatários explícitos.</summary>
    public IReadOnlyList<Guid>? RecipientUserIds { get; init; }

    /// <summary>Nomes dos papéis/roles destinatários (o orquestrador resolve para utilizadores).</summary>
    public IReadOnlyList<string>? RecipientRoles { get; init; }

    /// <summary>Ids das equipas destinatárias (o orquestrador resolve para utilizadores).</summary>
    public IReadOnlyList<Guid>? RecipientTeamIds { get; init; }

    /// <summary>Payload adicional em JSON para templates e contexto rico.</summary>
    public string? PayloadJson { get; init; }

    /// <summary>Data de expiração opcional da notificação.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Resultado da submissão de notificação.
/// </summary>
public sealed record NotificationResult(bool Success, string? ErrorMessage = null)
{
    /// <summary>Ids das notificações criadas (uma por destinatário).</summary>
    public IReadOnlyList<Guid> NotificationIds { get; init; } = [];
}
