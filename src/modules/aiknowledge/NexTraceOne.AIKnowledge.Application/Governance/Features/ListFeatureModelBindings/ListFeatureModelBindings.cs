using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListFeatureModelBindings;

/// <summary>
/// Feature: ListFeatureModelBindings — lista vinculações feature → modelo do tenant atual.
/// </summary>
public static class ListFeatureModelBindings
{
    /// <summary>Query de listagem de vinculações do tenant.</summary>
    public sealed record Query(bool? IsActive) : IQuery<Response>;

    /// <summary>Handler que lista as vinculações do tenant atual.</summary>
    public sealed class Handler(
        IAiFeatureModelBindingRepository bindingRepository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var bindings = await bindingRepository.ListByTenantAsync(
                currentTenant.Id, request.IsActive, cancellationToken);

            var items = bindings.Select(b => new BindingDto(
                b.Id.Value,
                b.FeatureKey,
                b.Description,
                b.RequiredModelId,
                b.RequiredModelName,
                b.RequiredProviderId,
                b.FallbackModelId,
                b.FallbackModelName,
                b.FallbackProviderId,
                b.IsActive,
                b.CreatedAt,
                b.UpdatedAt)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de vinculações.</summary>
    public sealed record Response(IReadOnlyList<BindingDto> Items);

    /// <summary>DTO de vinculação feature → modelo.</summary>
    public sealed record BindingDto(
        Guid Id,
        string FeatureKey,
        string Description,
        Guid RequiredModelId,
        string RequiredModelName,
        string RequiredProviderId,
        Guid? FallbackModelId,
        string? FallbackModelName,
        string? FallbackProviderId,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
