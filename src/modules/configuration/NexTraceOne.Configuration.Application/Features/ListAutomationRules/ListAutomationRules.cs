using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListAutomationRules;

/// <summary>Feature: ListAutomationRules — lista as regras de automação do tenant.</summary>
public static class ListAutomationRules
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        IAutomationRuleRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var rules = await repository.ListByTenantAsync(currentTenant.Id.ToString(), cancellationToken);

            var items = rules
                .Select(r => new RuleSummary(
                    r.Id.Value,
                    r.Name,
                    r.Trigger,
                    r.ConditionsJson,
                    r.ActionsJson,
                    r.IsEnabled,
                    r.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record RuleSummary(
        Guid RuleId,
        string Name,
        string Trigger,
        string ConditionsJson,
        string ActionsJson,
        bool IsEnabled,
        DateTimeOffset CreatedAt);

    public sealed record Response(IReadOnlyList<RuleSummary> Items, int TotalCount);
}
