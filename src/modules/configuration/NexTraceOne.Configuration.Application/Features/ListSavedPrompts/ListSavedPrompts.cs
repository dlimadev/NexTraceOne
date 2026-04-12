using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListSavedPrompts;

/// <summary>Feature: ListSavedPrompts — lista os prompts guardados do utilizador autenticado.</summary>
public static class ListSavedPrompts
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        ISavedPromptRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var prompts = await repository.ListByUserAsync(currentUser.Id, currentTenant.Id.ToString(), cancellationToken);

            var items = prompts
                .Select(p => new PromptSummary(p.Id.Value, p.Name, p.PromptText, p.ContextType, p.TagsCsv, p.IsShared, p.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record PromptSummary(Guid PromptId, string Name, string PromptText, string ContextType, string? TagsCsv, bool IsShared, DateTimeOffset CreatedAt);
    public sealed record Response(IReadOnlyList<PromptSummary> Items, int TotalCount);
}
