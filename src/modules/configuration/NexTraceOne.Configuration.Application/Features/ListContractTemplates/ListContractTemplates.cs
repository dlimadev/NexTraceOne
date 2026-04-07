using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListContractTemplates;

/// <summary>Feature: ListContractTemplates — lista os templates de contrato do tenant, com filtro opcional por tipo.</summary>
public static class ListContractTemplates
{
    public sealed record Query(string? ContractType) : IQuery<Response>;

    public sealed class Handler(
        IContractTemplateRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var templates = await repository.ListByTenantAsync(
                currentTenant.Id.ToString(),
                request.ContractType,
                cancellationToken);

            var items = templates
                .Select(t => new TemplateSummary(
                    t.Id.Value,
                    t.Name,
                    t.ContractType,
                    t.Description,
                    t.IsBuiltIn,
                    t.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record TemplateSummary(
        Guid TemplateId,
        string Name,
        string ContractType,
        string Description,
        bool IsBuiltIn,
        DateTimeOffset CreatedAt);

    public sealed record Response(IReadOnlyList<TemplateSummary> Items, int TotalCount);
}
