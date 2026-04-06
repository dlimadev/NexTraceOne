using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Integrations.Application.Features.ListWebhookSubscriptions;

/// <summary>
/// Feature: ListWebhookSubscriptions — lista subscrições de webhook outbound de um tenant.
/// Retorna subscrições paginadas com suporte a filtro por estado activo/inactivo.
/// Handler nativo do módulo Integrations.
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

    /// <summary>Handler que retorna a lista paginada de subscrições de webhook.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private static readonly DateTimeOffset _baseDate = new DateTimeOffset(2025, 1, 10, 8, 0, 0, TimeSpan.Zero);

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var allItems = new List<WebhookSubscriptionDto>
            {
                new(
                    SubscriptionId: Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                    Name: "Incident Alerts — PagerDuty",
                    TargetUrl: "https://events.pagerduty.com/integration/v1/alerts",
                    EventTypes: new[] { "incident.created", "incident.resolved" },
                    HasSecret: true,
                    IsActive: true,
                    EventCount: 2,
                    CreatedAt: _baseDate,
                    LastTriggeredAt: _baseDate.AddDays(5)),

                new(
                    SubscriptionId: Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"),
                    Name: "Deploy Events — Slack",
                    TargetUrl: "https://hooks.slack.com/services/T00/B00/xxxx",
                    EventTypes: new[] { "change.deployed", "change.promoted", "contract.published" },
                    HasSecret: false,
                    IsActive: true,
                    EventCount: 3,
                    CreatedAt: _baseDate.AddDays(-3),
                    LastTriggeredAt: _baseDate.AddDays(2)),

                new(
                    SubscriptionId: Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003"),
                    Name: "Contract & Service Events — MS Teams",
                    TargetUrl: "https://myorg.webhook.office.com/webhookb2/xxxx",
                    EventTypes: new[] { "contract.deprecated", "service.registered", "alert.triggered" },
                    HasSecret: true,
                    IsActive: false,
                    EventCount: 3,
                    CreatedAt: _baseDate.AddDays(-10),
                    LastTriggeredAt: null),
            };

            var filtered = request.IsActive.HasValue
                ? allItems.Where(x => x.IsActive == request.IsActive.Value).ToList()
                : allItems;

            var totalCount = filtered.Count;
            var items = filtered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Task.FromResult(Result<Response>.Success(new Response(items, totalCount)));
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
