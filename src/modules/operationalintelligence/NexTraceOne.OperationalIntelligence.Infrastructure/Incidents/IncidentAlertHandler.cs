using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Integra alertas operacionais ao fluxo de incidentes.
/// Quando um alerta com severidade Error ou Critical é despachado,
/// cria automaticamente um incidente no sistema para garantir rastreabilidade
/// e resposta operacional.
///
/// Alertas Info e Warning são apenas registados em log — não geram incidentes.
/// </summary>
public sealed class IncidentAlertHandler(
    IIncidentStore incidentStore,
    ILogger<IncidentAlertHandler> logger) : IOperationalAlertHandler
{
    public Task HandleAlertAsync(
        AlertPayload payload,
        AlertDispatchResult dispatchResult,
        CancellationToken cancellationToken)
    {
        if (payload.Severity < AlertSeverity.Error)
        {
            logger.LogDebug(
                "Alert '{Title}' [{Severity}] below Error threshold — no incident created",
                payload.Title,
                payload.Severity);
            return Task.CompletedTask;
        }

        try
        {
            var input = new CreateIncidentInput(
                Title: $"[Alert] {payload.Title}",
                Description: BuildIncidentDescription(payload, dispatchResult),
                IncidentType: MapAlertSourceToIncidentType(payload.Source),
                Severity: MapAlertSeverity(payload.Severity),
                ServiceId: payload.Context.GetValueOrDefault("ServiceId", payload.Source),
                ServiceDisplayName: payload.Context.GetValueOrDefault("ServiceName", payload.Source),
                OwnerTeam: payload.Context.GetValueOrDefault("OwnerTeam", "platform"),
                ImpactedDomain: payload.Context.GetValueOrDefault("Domain"),
                Environment: payload.Context.GetValueOrDefault("Environment", "production"),
                DetectedAtUtc: payload.Timestamp);

            var result = incidentStore.CreateIncident(input);

            logger.LogInformation(
                "Incident {IncidentRef} created from alert '{AlertTitle}' [{Severity}]. CorrelationId: {CorrelationId}",
                result.Reference,
                payload.Title,
                payload.Severity,
                payload.CorrelationId ?? "none");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to create incident from alert '{Title}'. Alert was dispatched to {Channels} channel(s).",
                payload.Title,
                dispatchResult.TotalChannels);
        }

        return Task.CompletedTask;
    }

    private static string BuildIncidentDescription(AlertPayload payload, AlertDispatchResult dispatchResult)
    {
        var lines = new List<string>
        {
            payload.Description,
            "",
            $"**Source:** {payload.Source}",
            $"**Severity:** {payload.Severity}",
            $"**Timestamp:** {payload.Timestamp:O}",
            $"**Alert channels dispatched:** {dispatchResult.TotalChannels} (failed: {dispatchResult.FailedChannels})"
        };

        if (!string.IsNullOrEmpty(payload.CorrelationId))
        {
            lines.Add($"**Correlation ID:** {payload.CorrelationId}");
        }

        if (payload.Context.Count > 0)
        {
            lines.Add("");
            lines.Add("**Context:**");
            foreach (var (key, value) in payload.Context)
            {
                lines.Add($"- {key}: {value}");
            }
        }

        return string.Join("\n", lines);
    }

    private static IncidentType MapAlertSourceToIncidentType(string source) => source.ToLowerInvariant() switch
    {
        "health" or "health-check" or "platform-health" => IncidentType.AvailabilityIssue,
        "worker" or "background-jobs" or "scheduler" => IncidentType.BackgroundProcessingIssue,
        "ingestion" or "pipeline" => IncidentType.ServiceDegradation,
        "ai" or "ai-provider" => IncidentType.DependencyFailure,
        "drift" or "anomaly" or "change-intelligence" => IncidentType.OperationalRegression,
        _ => IncidentType.ServiceDegradation
    };

    private static IncidentSeverity MapAlertSeverity(AlertSeverity severity) => severity switch
    {
        AlertSeverity.Critical => IncidentSeverity.Critical,
        AlertSeverity.Error => IncidentSeverity.Major,
        AlertSeverity.Warning => IncidentSeverity.Minor,
        _ => IncidentSeverity.Warning
    };
}
