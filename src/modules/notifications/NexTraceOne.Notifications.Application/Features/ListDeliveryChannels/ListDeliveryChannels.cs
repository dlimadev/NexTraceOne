using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.ListDeliveryChannels;

/// <summary>
/// Feature: ListDeliveryChannels — lista as configurações de canais de entrega do tenant.
/// </summary>
public static class ListDeliveryChannels
{
    /// <summary>Query de listagem de canais de entrega.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler que lista as configurações de canais do tenant autenticado.</summary>
    public sealed class Handler(
        IDeliveryChannelConfigurationStore store,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var channels = await store.ListAsync(tenant.Id, cancellationToken);

            var items = channels
                .Select(c => new ChannelDto(
                    c.Id.Value,
                    c.ChannelType.ToString(),
                    c.DisplayName,
                    c.IsEnabled,
                    c.ConfigurationJson,
                    c.CreatedAt,
                    c.UpdatedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta com a lista de configurações de canais.</summary>
    public sealed record Response(IReadOnlyList<ChannelDto> Items);

    /// <summary>DTO de configuração de canal de entrega.</summary>
    public sealed record ChannelDto(
        Guid Id,
        string ChannelType,
        string DisplayName,
        bool IsEnabled,
        string? ConfigurationJson,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
