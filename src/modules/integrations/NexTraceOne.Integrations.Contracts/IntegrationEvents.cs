using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Integrations.Contracts;

/// <summary>
/// Eventos de integração publicados pelo módulo Integrations.
/// Utilizado por outros módulos para reagir a falhas e mudanças de estado de conectores e ingestão.
///
/// P2.5: Criado durante o alinhamento de contracts cross-module.
/// ConnectorAuthFailedIntegrationEvent e SyncFailedIntegrationEvent movidos de
/// OperationalIntelligence.Contracts para o bounded context correto.
/// </summary>
public static class IntegrationEvents
{
    /// <summary>
    /// Publicado quando autenticação de conector falha.
    /// Consumidores: módulo de notificações (alertar owner da integração).
    /// </summary>
    public sealed record ConnectorAuthFailedIntegrationEvent(
        Guid ConnectorId,
        string ConnectorName,
        string ErrorMessage,
        Guid? OwnerUserId,
        Guid? TenantId) : IntegrationEventBase("Integrations");

    /// <summary>
    /// Publicado quando sincronização/ingestão de um conector falha.
    /// Consumidores: módulo de notificações (alertar owner da integração).
    /// </summary>
    public sealed record SyncFailedIntegrationEvent(
        Guid IntegrationId,
        string IntegrationName,
        string ErrorMessage,
        Guid? OwnerUserId,
        Guid? TenantId) : IntegrationEventBase("Integrations");

    /// <summary>
    /// Publicado quando um conector é ativado com sucesso.
    /// Consumidores: módulo de notificações (informar owner).
    /// </summary>
    public sealed record ConnectorActivatedIntegrationEvent(
        Guid ConnectorId,
        string ConnectorName,
        Guid? TenantId) : IntegrationEventBase("Integrations");

    /// <summary>
    /// Publicado quando um conector é desativado.
    /// Consumidores: módulo de notificações (informar owner e administradores).
    /// </summary>
    public sealed record ConnectorDeactivatedIntegrationEvent(
        Guid ConnectorId,
        string ConnectorName,
        string Reason,
        Guid? TenantId) : IntegrationEventBase("Integrations");
}
