using HotChocolate.Types;
using HotChocolate.Subscriptions;
using NexTraceOne.Catalog.API.GraphQL.Types;

namespace NexTraceOne.Catalog.API.GraphQL;

/// <summary>
/// Tipo GraphQL de evento de mudança — payload enviado via WebSocket subscription.
/// Usado para notificações real-time de novos deploys e mudanças no catálogo.
/// Persona: Engineer, Tech Lead, Architect.
/// </summary>
public sealed class ChangeEventNotification
{
    /// <summary>Identificador do evento de mudança.</summary>
    public Guid ChangeId { get; init; }

    /// <summary>Nome do serviço afetado.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Ambiente alvo da mudança (Development, Pre-Production, Production).</summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>Tipo de evento (deployed, rolled-back, promoted).</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Nome da equipa responsável pela mudança.</summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>Data/hora UTC do evento.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Versão do serviço após a mudança.</summary>
    public string? Version { get; init; }
}

/// <summary>
/// Tipo GraphQL de evento de incidente — payload enviado via WebSocket subscription.
/// Usado para notificações real-time de novos incidentes e atualizações de estado.
/// Persona: Engineer, Tech Lead.
/// </summary>
public sealed class IncidentEventNotification
{
    /// <summary>Identificador do incidente.</summary>
    public Guid IncidentId { get; init; }

    /// <summary>Título do incidente.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Severidade do incidente (Low, Medium, High, Critical).</summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>Estado atual do incidente (Open, Investigating, Mitigating, Resolved).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Nome do serviço afetado.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Nome da equipa responsável pelo serviço afetado.</summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>Data/hora UTC de abertura do incidente.</summary>
    public DateTimeOffset OpenedAt { get; init; }
}

/// <summary>
/// Subscription GraphQL do NexTraceOne.
/// Expõe notificações real-time via WebSocket para mudanças em produção e incidentes ativos.
/// Usa o tópico HotChocolate in-memory para publicação e subscrição de eventos.
/// Persona: Engineer, Tech Lead.
/// </summary>
[ExtendObjectType("Subscription")]
public sealed class CatalogSubscription
{
    /// <summary>
    /// Subscrição real-time de eventos de mudanças.
    /// Notifica quando um deploy, rollback ou promoção ocorre num ambiente monitorizado.
    /// Filtra opcionalmente por ambiente e/ou nome de serviço.
    /// </summary>
    [Subscribe]
    [Topic(GraphQLTopics.ChangeEvents)]
    public ChangeEventNotification OnChangeDeployed(
        [EventMessage] ChangeEventNotification notification)
    {
        return notification;
    }

    /// <summary>
    /// Subscrição real-time de eventos de incidentes.
    /// Notifica quando um novo incidente é aberto ou o estado de um incidente existente muda.
    /// </summary>
    [Subscribe]
    [Topic(GraphQLTopics.IncidentEvents)]
    public IncidentEventNotification OnIncidentUpdated(
        [EventMessage] IncidentEventNotification notification)
    {
        return notification;
    }
}

/// <summary>
/// Constantes de tópicos de evento usados no GraphQL Federation Gateway.
/// Centraliza os nomes de tópicos para evitar erros de string literais.
/// </summary>
public static class GraphQLTopics
{
    /// <summary>Tópico para eventos de mudanças/deploys.</summary>
    public const string ChangeEvents = "onChangeDeployed";

    /// <summary>Tópico para eventos de incidentes.</summary>
    public const string IncidentEvents = "onIncidentUpdated";
}
