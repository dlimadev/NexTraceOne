using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetSubscriptions;

/// <summary>
/// Feature: GetSubscriptions — lista subscrições de API de um utilizador.
/// Retorna preferências de notificação, canal e estado de cada subscrição.
/// </summary>
public static class GetSubscriptions
{
    /// <summary>Query para listar subscrições de um utilizador.</summary>
    public sealed record Query(Guid SubscriberId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de subscrições.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriberId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna todas as subscrições ativas e inativas de um utilizador.</summary>
    public sealed class Handler(ISubscriptionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var subscriptions = await repository.GetBySubscriberAsync(request.SubscriberId, cancellationToken);

            var dtos = subscriptions.Select(s => new SubscriptionDto(
                s.Id.Value,
                s.ApiAssetId,
                s.ApiName,
                s.ConsumerServiceName,
                s.ConsumerServiceVersion,
                s.Level.ToString(),
                s.Channel.ToString(),
                s.WebhookUrl,
                s.IsActive,
                s.CreatedAt,
                s.LastNotifiedAt)).ToList();

            return new Response(dtos);
        }
    }

    /// <summary>DTO de subscrição de API para listagem.</summary>
    public sealed record SubscriptionDto(
        Guid SubscriptionId,
        Guid ApiAssetId,
        string ApiName,
        string ConsumerServiceName,
        string ConsumerServiceVersion,
        string Level,
        string Channel,
        string? WebhookUrl,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastNotifiedAt);

    /// <summary>Resposta com lista de subscrições do utilizador.</summary>
    public sealed record Response(IReadOnlyList<SubscriptionDto> Subscriptions);
}
