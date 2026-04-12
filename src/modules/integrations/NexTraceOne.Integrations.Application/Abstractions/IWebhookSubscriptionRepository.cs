using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Repositório de subscrições de webhook outbound.
/// Permite CRUD e consulta filtrada por tenant e estado.
/// </summary>
public interface IWebhookSubscriptionRepository
{
    /// <summary>Lista subscrições filtradas por estado activo/inactivo, com paginação.</summary>
    Task<(IReadOnlyList<WebhookSubscription> Items, int TotalCount)> ListAsync(
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Obtém uma subscrição pelo seu identificador.</summary>
    Task<WebhookSubscription?> GetByIdAsync(WebhookSubscriptionId id, CancellationToken ct);

    /// <summary>Adiciona uma nova subscrição.</summary>
    Task AddAsync(WebhookSubscription subscription, CancellationToken ct);

    /// <summary>Atualiza uma subscrição existente.</summary>
    Task UpdateAsync(WebhookSubscription subscription, CancellationToken ct);
}
