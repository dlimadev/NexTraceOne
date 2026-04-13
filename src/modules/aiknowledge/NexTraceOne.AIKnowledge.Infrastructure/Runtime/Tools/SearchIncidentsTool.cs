using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: pesquisa incidentes por serviço, severidade, estado ou intervalo temporal.
/// Consulta o módulo de OperationalIntelligence via IIncidentGroundingReader para retornar
/// incidentes relevantes para o contexto de análise do agent.
/// </summary>
public sealed class SearchIncidentsTool : IAgentTool
{
    private readonly IIncidentGroundingReader _incidentReader;
    private readonly ILogger<SearchIncidentsTool> _logger;

    public SearchIncidentsTool(
        IIncidentGroundingReader incidentReader,
        ILogger<SearchIncidentsTool> logger)
    {
        _incidentReader = incidentReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "search_incidents",
        "Searches incidents by service, severity, status, or time range. Returns recent incidents with context for analysis.",
        "operations",
        [
            new ToolParameterDefinition("serviceId", "Filter by service identifier or name", "string"),
            new ToolParameterDefinition("severity", "Filter by severity: Critical, High, Medium, Low", "string"),
            new ToolParameterDefinition("environment", "Filter by environment (default: production)", "string"),
            new ToolParameterDefinition("days", "Number of days to look back (default: 7)", "integer"),
            new ToolParameterDefinition("limit", "Maximum number of results (default: 10)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            _logger.LogInformation(
                "SearchIncidentsTool executing for serviceId={ServiceId}, severity={Severity}, days={Days}",
                args.ServiceId, args.Severity, args.Days);

            var from = DateTimeOffset.UtcNow.AddDays(-args.Days);

            var incidents = await _incidentReader.FindRecentIncidentsAsync(
                from: from,
                serviceId: args.ServiceId,
                environment: args.Environment,
                maxResults: args.Limit,
                ct: cancellationToken);

            // Apply severity filter post-retrieval (grounding reader does not filter by severity)
            var filtered = string.IsNullOrWhiteSpace(args.Severity)
                ? incidents
                : incidents
                    .Where(i => string.Equals(i.Severity, args.Severity, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var result = new
            {
                tool = "search_incidents",
                status = "executed",
                filters = new
                {
                    serviceId = args.ServiceId,
                    severity = args.Severity,
                    environment = args.Environment ?? "all",
                    days = args.Days,
                    limit = args.Limit,
                },
                total = filtered.Count,
                incidents = filtered.Select(i => new
                {
                    incidentId = i.IncidentId,
                    title = i.Title,
                    service = i.ServiceName,
                    severity = i.Severity,
                    status = i.Status,
                    environment = i.Environment,
                    description = i.Description,
                    detectedAt = i.DetectedAt,
                }),
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "search_incidents", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SearchIncidentsTool failed");
            return new ToolExecutionResult(
                false, "search_incidents", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static SearchIncidentsArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SearchIncidentsArgs(null, null, null, 7, 10);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var serviceId = root.TryGetProperty("serviceId", out var svcProp) ? svcProp.GetString() : null;
            var severity = root.TryGetProperty("severity", out var sevProp) ? sevProp.GetString() : null;
            var env = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var days = root.TryGetProperty("days", out var daysProp) && daysProp.TryGetInt32(out var d) ? d : 7;
            var limit = root.TryGetProperty("limit", out var limitProp) && limitProp.TryGetInt32(out var l) ? l : 10;

            return new SearchIncidentsArgs(serviceId, severity, env, days, limit);
        }
        catch
        {
            return new SearchIncidentsArgs(null, null, null, 7, 10);
        }
    }

    private sealed record SearchIncidentsArgs(
        string? ServiceId,
        string? Severity,
        string? Environment,
        int Days,
        int Limit);
}
