using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application.Features.ListWebhookSubscriptions;

/// <summary>
/// Feature: ListWebhookSubscriptions — lista subscrições de webhook outbound de um tenant.
/// Retorna subscrições paginadas com suporte a filtro por estado activo/inactivo.
/// Consulta dados reais via IWebhookSubscriptionRepository.
/// Ownership: módulo Integrations.
/// </summary>
public static class ListWebhookSubscriptions
{
    /// <summary>Query para listar subscrições de webhook com filtros e paginação.</summary>
    public sealed record Query(
        string TenantId,
        bool? IsActive = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validador da query ListWebhookSubscriptions.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }

    /// <summary>Handler que retorna a lista paginada de subscrições de webhook via repositório real.</summary>
    public sealed class Handler(IWebhookSubscriptionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (subscriptions, totalCount) = await repository.ListAsync(
                request.IsActive,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = subscriptions.Select(s => new WebhookSubscriptionDto(
                SubscriptionId: s.Id.Value,
                Name: s.Name,
                TargetUrl: s.TargetUrl,
                EventTypes: s.EventTypes,
                HasSecret: s.SecretHash is not null,
                IsActive: s.IsActive,
                EventCount: s.EventTypes.Count,
                CreatedAt: s.CreatedAt,
                LastTriggeredAt: s.LastTriggeredAt)).ToList();

            return Result<Response>.Success(new Response(items, totalCount));
        }
    }

    /// <summary>Resposta da query ListWebhookSubscriptions.</summary>
    public sealed record Response(
        IReadOnlyList<WebhookSubscriptionDto> Items,
        int TotalCount);

    /// <summary>DTO representando uma subscrição de webhook outbound.</summary>
    public sealed record WebhookSubscriptionDto(
        Guid SubscriptionId,
        string Name,
        string TargetUrl,
        IReadOnlyList<string> EventTypes,
        bool HasSecret,
        bool IsActive,
        int EventCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastTriggeredAt);
}
