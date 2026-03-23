using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Implementação real de IPlatformHealthProvider que consulta os health checks registados
/// no ASP.NET Core (HealthCheckService) para obter o estado verdadeiro de cada subsistema.
///
/// Agrupa health checks por subsistema lógico:
/// - Database: todos os checks de conectividade de BD (*-db)
/// - AI: checks de providers de IA (ai-providers, ai-*-db)
/// - API: estado implícito — se o handler executa, a API está funcional
/// - BackgroundJobs: reportado como Unknown (sem health check dedicado nesta fase)
/// - Ingestion: reportado como Unknown (sem health check dedicado nesta fase)
/// </summary>
internal sealed class HealthCheckPlatformHealthProvider(HealthCheckService healthCheckService) : IPlatformHealthProvider
{
    private static readonly IReadOnlyDictionary<string, string[]> SubsystemCheckMapping = new Dictionary<string, string[]>
    {
        ["Database"] =
        [
            "identity-db", "catalog-graph-db", "contracts-db", "change-intelligence-db",
            "runtime-intelligence-db", "governance-db", "ruleset-governance-db",
            "workflow-db", "promotion-db", "developer-portal-db", "incident-db",
            "cost-intelligence-db", "audit-db"
        ],
        ["AI"] = ["ai-providers", "ai-governance-db", "external-ai-db", "ai-orchestration-db"],
    };

    public async Task<IReadOnlyList<SubsystemHealthInfo>> GetSubsystemHealthAsync(CancellationToken cancellationToken)
    {
        var subsystems = new List<SubsystemHealthInfo>();

        // API: se estamos a executar, a API está funcional
        subsystems.Add(new SubsystemHealthInfo(
            "API",
            PlatformSubsystemStatus.Healthy,
            "API host is responding."));

        try
        {
            var report = await healthCheckService.CheckHealthAsync(cancellationToken);

            // Database subsystem — agregar saúde de todas as BDs
            subsystems.Add(BuildSubsystemHealth("Database", SubsystemCheckMapping["Database"], report));

            // AI subsystem — agregar saúde dos providers de IA e BDs de IA
            subsystems.Add(BuildSubsystemHealth("AI", SubsystemCheckMapping["AI"], report));
        }
        catch (OperationCanceledException)
        {
            subsystems.Add(new SubsystemHealthInfo("Database", PlatformSubsystemStatus.Unknown, "Health check cancelled."));
            subsystems.Add(new SubsystemHealthInfo("AI", PlatformSubsystemStatus.Unknown, "Health check cancelled."));
        }
        catch (Exception)
        {
            subsystems.Add(new SubsystemHealthInfo("Database", PlatformSubsystemStatus.Unknown, "Health check failed."));
            subsystems.Add(new SubsystemHealthInfo("AI", PlatformSubsystemStatus.Unknown, "Health check failed."));
        }

        // BackgroundJobs e Ingestion — sem health check dedicado, reportar como Unknown
        subsystems.Add(new SubsystemHealthInfo(
            "BackgroundJobs",
            PlatformSubsystemStatus.Unknown,
            "No dedicated health check available. Status not evaluated."));

        subsystems.Add(new SubsystemHealthInfo(
            "Ingestion",
            PlatformSubsystemStatus.Unknown,
            "No dedicated health check available. Status not evaluated."));

        return subsystems;
    }

    private static SubsystemHealthInfo BuildSubsystemHealth(
        string name,
        string[] checkNames,
        HealthReport report)
    {
        var matchedEntries = report.Entries
            .Where(entry => checkNames.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (matchedEntries.Count == 0)
        {
            return new SubsystemHealthInfo(name, PlatformSubsystemStatus.Unknown,
                "No health checks matched for this subsystem.");
        }

        var unhealthy = matchedEntries
            .Where(e => e.Value.Status == HealthStatus.Unhealthy)
            .Select(e => e.Key)
            .ToArray();

        var degraded = matchedEntries
            .Where(e => e.Value.Status == HealthStatus.Degraded)
            .Select(e => e.Key)
            .ToArray();

        if (unhealthy.Length > 0)
        {
            return new SubsystemHealthInfo(name, PlatformSubsystemStatus.Unhealthy,
                $"Unhealthy checks: {string.Join(", ", unhealthy)}.");
        }

        if (degraded.Length > 0)
        {
            return new SubsystemHealthInfo(name, PlatformSubsystemStatus.Degraded,
                $"Degraded checks: {string.Join(", ", degraded)}.");
        }

        return new SubsystemHealthInfo(name, PlatformSubsystemStatus.Healthy,
            $"All {matchedEntries.Count} checks healthy.");
    }
}
