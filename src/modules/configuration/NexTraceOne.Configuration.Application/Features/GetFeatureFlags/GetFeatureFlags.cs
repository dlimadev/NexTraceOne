using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;

namespace NexTraceOne.Configuration.Application.Features.GetFeatureFlags;

/// <summary>
/// Feature: GetFeatureFlags — returns all registered feature flag definitions.
/// Used by management UI and services to discover available feature flags.
/// </summary>
public static class GetFeatureFlags
{
    /// <summary>Query to retrieve all feature flag definitions.</summary>
    public sealed record Query : IQuery<List<FeatureFlagDefinitionDto>>;

    /// <summary>Handler that fetches all flag definitions from the repository and maps to DTOs.</summary>
    public sealed class Handler(IFeatureFlagRepository repository)
        : IQueryHandler<Query, List<FeatureFlagDefinitionDto>>
    {
        public async Task<Result<List<FeatureFlagDefinitionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var definitions = await repository.GetAllDefinitionsAsync(cancellationToken);

            var dtos = definitions.Select(d => new FeatureFlagDefinitionDto(
                Id: d.Id.Value,
                Key: d.Key,
                DisplayName: d.DisplayName,
                Description: d.Description,
                DefaultEnabled: d.DefaultEnabled,
                AllowedScopes: d.AllowedScopes.Select(s => s.ToString()).ToArray(),
                ModuleId: d.ModuleId?.Value,
                IsActive: d.IsActive,
                IsEditable: d.IsEditable)).ToList();

            return dtos;
        }
    }
}
