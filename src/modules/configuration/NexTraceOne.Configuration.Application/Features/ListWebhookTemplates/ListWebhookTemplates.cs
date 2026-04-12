using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListWebhookTemplates;

/// <summary>Feature: ListWebhookTemplates — lista os templates de webhook do tenant autenticado.</summary>
public static class ListWebhookTemplates
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        IWebhookTemplateRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var templates = await repository.ListByTenantAsync(currentTenant.Id.ToString(), cancellationToken);

            var items = templates
                .Select(t => new TemplateSummary(t.Id.Value, t.Name, t.EventType, t.IsEnabled, t.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record TemplateSummary(Guid TemplateId, string Name, string EventType, bool IsEnabled, DateTimeOffset CreatedAt);
    public sealed record Response(IReadOnlyList<TemplateSummary> Items, int TotalCount);
}
