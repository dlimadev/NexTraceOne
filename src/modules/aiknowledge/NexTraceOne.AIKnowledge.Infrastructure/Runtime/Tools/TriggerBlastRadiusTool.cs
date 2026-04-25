using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: calcula o blast radius de uma mudança proposta verificando releases recentes
/// e serviços dependentes via IChangeGroundingReader.
/// </summary>
public sealed class TriggerBlastRadiusTool : IAgentTool
{
    private readonly IChangeGroundingReader _changeReader;
    private readonly ICatalogGroundingReader _catalogReader;
    private readonly ILogger<TriggerBlastRadiusTool> _logger;

    public TriggerBlastRadiusTool(
        IChangeGroundingReader changeReader,
        ICatalogGroundingReader catalogReader,
        ILogger<TriggerBlastRadiusTool> logger)
    {
        _changeReader = changeReader;
        _catalogReader = catalogReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "trigger_blast_radius",
        "Calculates the blast radius of a proposed change by examining recent releases, dependencies and risk score.",
        "change_intelligence",
        [
            new ToolParameterDefinition("serviceId", "Service identifier to analyse", "string", required: true),
            new ToolParameterDefinition("environment", "Target environment (default: production)", "string"),
            new ToolParameterDefinition("days", "Look-back window in days (default: 7)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (serviceId, environment, days) = ParseArgs(argumentsJson);

            _logger.LogInformation(
                "TriggerBlastRadiusTool: service={Service}, env={Env}, days={Days}",
                serviceId, environment, days);

            var from = DateTimeOffset.UtcNow.AddDays(-days);
            var to = DateTimeOffset.UtcNow;

            // Parallel: recent releases + service graph
            var releasesTask = _changeReader.FindRecentReleasesAsync(
                from, to, serviceId, environment, null, 20, cancellationToken);
            var servicesTask = _catalogReader.FindServicesAsync(serviceId, serviceId, 10, cancellationToken);

            await Task.WhenAll(releasesTask, servicesTask);

            var releases = releasesTask.Result;
            var services = servicesTask.Result;

            var avgScore = releases.Count > 0
                ? releases.Average(r => (double)r.ChangeScore)
                : 0.0;

            var riskLevel = avgScore switch
            {
                >= 0.8 => "Critical",
                >= 0.6 => "High",
                >= 0.4 => "Medium",
                _ => "Low",
            };

            var result = new
            {
                tool = "trigger_blast_radius",
                serviceId,
                environment,
                windowDays = days,
                riskLevel,
                averageChangeScore = Math.Round(avgScore, 3),
                recentReleases = releases.Count,
                potentiallyAffectedServices = services.Count,
                services = services.Select(s => new { s.ServiceId, s.DisplayName, s.Criticality }),
                releases = releases.Take(5).Select(r => new
                {
                    r.ReleaseId,
                    r.Version,
                    r.Status,
                    r.ChangeLevel,
                    changeScore = r.ChangeScore,
                }),
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "trigger_blast_radius",
                JsonSerializer.Serialize(result),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "TriggerBlastRadiusTool failed");
            return new ToolExecutionResult(false, "trigger_blast_radius", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static (string serviceId, string environment, int days) ParseArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ("", "production", 7);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var svc = root.TryGetProperty("serviceId", out var s) ? s.GetString() ?? "" : "";
            var env = root.TryGetProperty("environment", out var e) ? e.GetString() ?? "production" : "production";
            var days = root.TryGetProperty("days", out var d) && d.TryGetInt32(out var v) ? v : 7;
            return (svc, env, Math.Clamp(days, 1, 90));
        }
        catch { return ("", "production", 7); }
    }
}
