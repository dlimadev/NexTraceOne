using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Services;

/// <summary>
/// Implementação padrão de <see cref="IVariableValueResolver"/>.
/// Resolve valores de variáveis consultando repositórios locais ou retornando valores simulados
/// quando os módulos downstream não estão disponíveis (honest gap).
/// </summary>
public sealed class VariableValueResolver : IVariableValueResolver
{
    public Task<IReadOnlyList<string>> ResolveAsync(
        DashboardVariableType type,
        DashboardVariableSource source,
        IReadOnlyList<string>? staticValues,
        string tenantId,
        string? environmentId,
        CancellationToken cancellationToken)
    {
        // Static values — return as-is
        if (source == DashboardVariableSource.Static && staticValues is not null)
            return Task.FromResult<IReadOnlyList<string>>(staticValues.ToList());

        // Environment variable
        if (type == DashboardVariableType.Environment)
        {
            // Honest gap: no environment repository available yet
            // Return common environment names as simulated values
            var envs = new[] { "production", "staging", "development", "qa", "demo" };
            return Task.FromResult<IReadOnlyList<string>>(envs.ToList());
        }

        // Service variable — would query Catalog module in full implementation
        if (type == DashboardVariableType.Service)
        {
            // Simulated catalog services (honest gap until real Catalog bridge)
            var services = new[]
            {
                "api-gateway", "auth-service", "billing-service", "catalog-service",
                "notification-service", "order-service", "payment-service", "user-service",
                "inventory-service", "analytics-service", "search-service", "web-frontend"
            };
            return Task.FromResult<IReadOnlyList<string>>(services.ToList());
        }

        // Team variable — would query Governance teams in full implementation
        if (type == DashboardVariableType.Team)
        {
            var teams = new[]
            {
                "platform-team", "sre-team", "backend-team", "frontend-team",
                "data-team", "mobile-team", "security-team", "qa-team"
            };
            return Task.FromResult<IReadOnlyList<string>>(teams.ToList());
        }

        // TimeRange variable
        if (type == DashboardVariableType.TimeRange)
        {
            var ranges = new[] { "1h", "6h", "24h", "7d", "30d", "90d" };
            return Task.FromResult<IReadOnlyList<string>>(ranges.ToList());
        }

        // Default fallback
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
