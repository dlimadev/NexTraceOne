using HotChocolate.Subscriptions;
using NexTraceOne.Catalog.API.GraphQL;

namespace NexTraceOne.Catalog.API.GraphQL.Publishers;

/// <summary>
/// Contrato para publicação de eventos no GraphQL Federation Gateway.
/// Permite que outros módulos publiquem eventos sem depender diretamente do HotChocolate.
/// </summary>
public interface IGraphQLEventPublisher
{
    /// <summary>Publica um evento de mudança para todos os subscritores do tópico.</summary>
    Task PublishChangeEventAsync(ChangeEventNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Publica um evento de incidente para todos os subscritores do tópico.</summary>
    Task PublishIncidentEventAsync(IncidentEventNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação do publisher de eventos GraphQL usando HotChocolate in-memory event sender.
/// Publica para os tópicos definidos em <see cref="GraphQLTopics"/>.
/// </summary>
public sealed class GraphQLEventPublisher(ITopicEventSender eventSender) : IGraphQLEventPublisher
{
    /// <inheritdoc/>
    public async Task PublishChangeEventAsync(ChangeEventNotification notification, CancellationToken cancellationToken = default)
    {
        await eventSender.SendAsync(GraphQLTopics.ChangeEvents, notification, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishIncidentEventAsync(IncidentEventNotification notification, CancellationToken cancellationToken = default)
    {
        await eventSender.SendAsync(GraphQLTopics.IncidentEvents, notification, cancellationToken);
    }
}
