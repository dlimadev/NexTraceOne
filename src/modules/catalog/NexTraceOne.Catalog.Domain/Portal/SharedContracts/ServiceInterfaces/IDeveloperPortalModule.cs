namespace NexTraceOne.DeveloperPortal.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo DeveloperPortal.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
///
/// Decisão de design:
/// - Retorna tipos primitivos ou DTOs do Contracts layer para manter isolamento.
/// - Métodos são read-only (queries) para minimizar acoplamento entre módulos.
/// - Mutations no portal são feitas exclusivamente via MediatR (CQRS).
/// </summary>
public interface IDeveloperPortalModule
{
    /// <summary>
    /// Verifica se existe pelo menos uma subscrição ativa para um dado API asset.
    /// Útil para módulos como ChangeIntelligence determinarem se há consumidores registados
    /// a notificar quando uma breaking change é detetada.
    /// </summary>
    Task<bool> HasActiveSubscriptionsAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o número total de subscrições ativas para um dado API asset.
    /// Permite ao módulo EngineeringGraph ou Contracts exibir informações
    /// de popularidade de uma API no catálogo.
    /// </summary>
    Task<int> GetActiveSubscriptionCountAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém os identificadores dos subscritores ativos de um dado API asset.
    /// Utilizado pelo módulo de notificações para determinar quem deve receber
    /// alertas de mudanças, deprecações ou incidentes de segurança.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSubscriberIdsAsync(Guid apiAssetId, CancellationToken cancellationToken);
}
