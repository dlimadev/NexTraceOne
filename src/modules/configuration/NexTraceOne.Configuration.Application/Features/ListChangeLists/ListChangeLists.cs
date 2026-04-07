using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListChangeLists;

/// <summary>Feature: ListChangeLists — lista as checklists de mudança do tenant.</summary>
public static class ListChangeLists
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        IChangeChecklistRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var items = await repository.ListByTenantAsync(currentTenant.Id.ToString(), cancellationToken);

            var summaries = items
                .Select(c => new ChecklistSummary(
                    c.Id.Value,
                    c.Name,
                    c.ChangeType,
                    c.Environment,
                    c.IsRequired,
                    c.Items,
                    c.CreatedAt))
                .ToList();

            return new Response(summaries, summaries.Count);
        }
    }

    public sealed record ChecklistSummary(
        Guid ChecklistId,
        string Name,
        string ChangeType,
        string? Environment,
        bool IsRequired,
        IReadOnlyList<string> Items,
        DateTimeOffset CreatedAt);

    public sealed record Response(IReadOnlyList<ChecklistSummary> Items, int TotalCount);
}
