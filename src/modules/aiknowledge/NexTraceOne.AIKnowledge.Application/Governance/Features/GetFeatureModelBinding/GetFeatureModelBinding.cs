using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetFeatureModelBinding;

/// <summary>
/// Feature: GetFeatureModelBinding — obtém uma vinculação feature → modelo por ID.
/// </summary>
public static class GetFeatureModelBinding
{
    /// <summary>Query de obtenção de vinculação por ID.</summary>
    public sealed record Query(Guid BindingId) : IQuery<Response>;

    /// <summary>Handler que obtém a vinculação pelo ID.</summary>
    public sealed class Handler(
        IAiFeatureModelBindingRepository bindingRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var binding = await bindingRepository.GetByIdAsync(
                AiFeatureModelBindingId.From(request.BindingId), cancellationToken);

            if (binding is null)
                return Error.NotFound(
                    "AiFeatureModelBinding.NotFound",
                    "Vinculação '{0}' não encontrada.",
                    request.BindingId);

            return new Response(
                binding.Id.Value,
                binding.FeatureKey,
                binding.Description,
                binding.TenantId,
                binding.RequiredModelId,
                binding.RequiredModelName,
                binding.RequiredProviderId,
                binding.FallbackModelId,
                binding.FallbackModelName,
                binding.FallbackProviderId,
                binding.IsActive,
                binding.CreatedAt,
                binding.UpdatedAt);
        }
    }

    /// <summary>Resposta com os detalhes da vinculação.</summary>
    public sealed record Response(
        Guid Id,
        string FeatureKey,
        string Description,
        Guid TenantId,
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
