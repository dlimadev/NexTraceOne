using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.GetEffectiveSettings;

/// <summary>
/// Feature: GetEffectiveSettings — resolves effective configuration values for a scope.
/// When Key is provided, returns a single resolved value.
/// When Key is null, returns all effective settings for the scope.
/// </summary>
public static class GetEffectiveSettings
{
    /// <summary>Query to resolve effective configuration values.</summary>
    public sealed record Query(
        string? Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId) : IQuery<Response>;

    /// <summary>Handler that delegates resolution to the IConfigurationResolutionService.</summary>
    public sealed class Handler(
        IConfigurationResolutionService resolutionService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (request.Key is not null)
            {
                var effective = await resolutionService.ResolveEffectiveValueAsync(
                    request.Key,
                    request.Scope,
                    request.ScopeReferenceId,
                    cancellationToken);

                if (effective is null)
                    return Error.NotFound(
                        "CONFIG_KEY_NOT_FOUND",
                        "No effective configuration found for key '{0}' in scope '{1}'.",
                        request.Key,
                        request.Scope.ToString());

                return new Response(Setting: effective, Settings: null);
            }

            var allEffective = await resolutionService.ResolveAllEffectiveAsync(
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            return new Response(Setting: null, Settings: allEffective);
        }
    }

    /// <summary>
    /// Response containing either a single effective setting or a list of all effective settings.
    /// </summary>
    public sealed record Response(
        EffectiveConfigurationDto? Setting,
        List<EffectiveConfigurationDto>? Settings);
}
