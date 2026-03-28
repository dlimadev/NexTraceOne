using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Services;

internal sealed class ExternalAiModule(ExternalAiDbContext context) : IExternalAiModule
{
    private static readonly IReadOnlyList<string> DefaultCapabilities =
    [
        "change-analysis",
        "error-diagnosis",
        "test-generation"
    ];

    public async Task<IReadOnlyList<ProviderSummaryDto>> GetAvailableProvidersAsync(CancellationToken ct = default)
    {
        var activePolicies = await context.Policies
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.AllowedContexts)
            .ToListAsync(ct);

        var policyCapabilities = ExtractCapabilities(activePolicies);
        var resolvedCapabilities = policyCapabilities.Count > 0 ? policyCapabilities : DefaultCapabilities;

        return await context.Providers
            .AsNoTracking()
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .Select(p => new ProviderSummaryDto(
                p.Id.Value,
                p.Name,
                p.ModelName,
                p.IsActive ? "Healthy" : "Unhealthy",
                resolvedCapabilities,
                p.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<ProviderHealthDto?> GetProviderHealthAsync(Guid providerId, CancellationToken ct = default)
    {
        return await context.Providers
            .AsNoTracking()
            .Where(p => p.Id == Domain.ExternalAI.Entities.ExternalAiProviderId.From(providerId))
            .Select(p => new ProviderHealthDto(
                p.Id.Value,
                p.IsActive ? "Healthy" : "Unhealthy",
                null,
                p.UpdatedAt,
                p.IsActive ? null : "Provider is deactivated."))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<RoutingDecisionDto?> RouteRequestAsync(
        string capability,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        var activePolicies = await context.Policies
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var requiresApproval = activePolicies.Any(p => p.RequiresApproval && p.IsContextAllowed(capability));

        if (requiresApproval)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferredProvider))
        {
            var preferred = await context.Providers
                .AsNoTracking()
                .Where(p => p.IsActive && p.Name == preferredProvider)
                .OrderBy(p => p.Priority)
                .FirstOrDefaultAsync(ct);

            if (preferred is not null)
            {
                var fallback = await context.Providers
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.Id != preferred.Id)
                    .OrderBy(p => p.Priority)
                    .Select(p => (Guid?)p.Id.Value)
                    .FirstOrDefaultAsync(ct);

                return new RoutingDecisionDto(
                    preferred.Id.Value,
                    preferred.Name,
                    "Preferred provider selected.",
                    fallback);
            }
        }

        var selected = await context.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .FirstOrDefaultAsync(ct);

        if (selected is null)
        {
            return null;
        }

        var selectedFallback = await context.Providers
            .AsNoTracking()
            .Where(p => p.IsActive && p.Id != selected.Id)
            .OrderBy(p => p.Priority)
            .Select(p => (Guid?)p.Id.Value)
            .FirstOrDefaultAsync(ct);

        return new RoutingDecisionDto(
            selected.Id.Value,
            selected.Name,
            "Selected highest-priority active provider.",
            selectedFallback);
    }

    private static IReadOnlyList<string> ExtractCapabilities(IEnumerable<string> allowedContextsValues)
    {
        return allowedContextsValues
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
