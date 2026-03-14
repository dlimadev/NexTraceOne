using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetApiConsumers;

/// <summary>
/// Feature: GetApiConsumers — lista consumidores formais de uma API.
/// Combina subscrições do portal com dados do Catalog Graph.
/// Permite ao produtor ver quem consome e como.
/// </summary>
public static class GetApiConsumers
{
    /// <summary>Query para listar consumidores de uma API.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de consumidores.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna consumidores de uma API via subscrições registadas.
    /// Em produção, complementa com dados do Catalog Graph para dependências reais.
    /// </summary>
    public sealed class Handler(ISubscriptionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var subscriptions = await repository.GetByApiAssetAsync(request.ApiAssetId, cancellationToken);

            var consumers = subscriptions.Select(s => new ConsumerDto(
                s.SubscriberId,
                s.SubscriberEmail,
                s.ConsumerServiceName,
                s.ConsumerServiceVersion,
                s.Level.ToString(),
                s.Channel.ToString(),
                s.IsActive,
                s.CreatedAt)).ToList();

            return new Response(consumers, consumers.Count);
        }
    }

    /// <summary>DTO de consumidor de API com dados de subscrição.</summary>
    public sealed record ConsumerDto(
        Guid SubscriberId,
        string SubscriberEmail,
        string ConsumerServiceName,
        string ConsumerServiceVersion,
        string SubscriptionLevel,
        string NotificationChannel,
        bool IsActive,
        DateTimeOffset SubscribedAt);

    /// <summary>Resposta com lista de consumidores da API.</summary>
    public sealed record Response(IReadOnlyList<ConsumerDto> Consumers, int TotalCount);
}
