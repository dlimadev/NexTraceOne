using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.GetEffectiveFeatureFlag;

/// <summary>
/// Feature: GetEffectiveFeatureFlag — resolves the effective value of a feature flag for a given scope.
/// Applies hierarchical resolution: User → Team → Role → Environment → Tenant → System → DefaultEnabled.
/// When Key is null, returns all flags resolved for the given scope.
/// </summary>
public static class GetEffectiveFeatureFlag
{
    /// <summary>Query to resolve the effective value of a feature flag.</summary>
    public sealed record Query(
        string? Key,
        ConfigurationScope Scope,
        string? ScopeReferenceId) : IQuery<Response>;

    /// <summary>Handler that resolves flag values using hierarchical scope inheritance.</summary>
    public sealed class Handler(IFeatureFlagRepository repository)
        : IQueryHandler<Query, Response>
    {
        /// <summary>
        /// Scope hierarchy from most specific to most generic.
        /// Resolution stops at the first active entry found.
        /// </summary>
        private static readonly ConfigurationScope[] ScopeHierarchy =
        [
            ConfigurationScope.User,
            ConfigurationScope.Team,
            ConfigurationScope.Role,
            ConfigurationScope.Environment,
            ConfigurationScope.Tenant,
            ConfigurationScope.System
        ];

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (request.Key is not null)
            {
                var evaluated = await ResolveAsync(request.Key, request.Scope, request.ScopeReferenceId, cancellationToken);
                if (evaluated is null)
                    return Error.NotFound(
                        "FEATURE_FLAG_NOT_FOUND",
                        "No feature flag found for key '{0}'.",
                        request.Key);

                return new Response(Flag: evaluated, Flags: null);
            }

            var definitions = await repository.GetAllDefinitionsAsync(cancellationToken);
            var results = new List<EvaluatedFeatureFlagDto>();

            foreach (var definition in definitions.Where(d => d.IsActive))
            {
                var evaluated = await ResolveAsync(definition.Key, request.Scope, request.ScopeReferenceId, cancellationToken);
                if (evaluated is not null)
                    results.Add(evaluated);
            }

            return new Response(Flag: null, Flags: results);
        }

        private async Task<EvaluatedFeatureFlagDto?> ResolveAsync(
            string key,
            ConfigurationScope scope,
            string? scopeReferenceId,
            CancellationToken cancellationToken)
        {
            var definition = await repository.GetDefinitionByKeyAsync(key, cancellationToken);
            if (definition is null || !definition.IsActive)
                return null;

            var entries = await repository.GetAllEntriesByKeyAsync(key, cancellationToken);
            var startIndex = Array.IndexOf(ScopeHierarchy, scope);
            if (startIndex < 0) startIndex = 0;

            for (var i = startIndex; i < ScopeHierarchy.Length; i++)
            {
                var currentScope = ScopeHierarchy[i];

                if (!definition.AllowedScopes.Contains(currentScope))
                    continue;

                var scopeRef = currentScope == scope ? scopeReferenceId : null;

                var entry = entries.FirstOrDefault(e =>
                    e.Scope == currentScope
                    && e.ScopeReferenceId == scopeRef
                    && e.IsActive);

                if (entry is not null)
                {
                    return new EvaluatedFeatureFlagDto(
                        Key: key,
                        IsEnabled: entry.IsEnabled,
                        ResolvedScope: currentScope.ToString(),
                        ResolvedScopeReferenceId: scopeRef is not null
                            ? Guid.TryParse(scopeRef, out var parsed) ? parsed : null
                            : null,
                        IsInherited: currentScope != scope,
                        IsDefault: false,
                        DisplayName: definition.DisplayName,
                        Description: definition.Description);
                }
            }

            // Fall back to the definition's default value
            return new EvaluatedFeatureFlagDto(
                Key: key,
                IsEnabled: definition.DefaultEnabled,
                ResolvedScope: ConfigurationScope.System.ToString(),
                ResolvedScopeReferenceId: null,
                IsInherited: scope != ConfigurationScope.System,
                IsDefault: true,
                DisplayName: definition.DisplayName,
                Description: definition.Description);
        }
    }

    /// <summary>
    /// Response containing either a single resolved flag or all resolved flags for the scope.
    /// </summary>
    public sealed record Response(
        EvaluatedFeatureFlagDto? Flag,
        List<EvaluatedFeatureFlagDto>? Flags);
}
