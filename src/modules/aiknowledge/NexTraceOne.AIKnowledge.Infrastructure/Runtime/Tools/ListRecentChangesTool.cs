using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: lista mudanças recentes registadas no módulo Change Governance.
/// Retorna informação sobre deploys, releases e mudanças em produção via IChangeGroundingReader.
/// </summary>
public sealed class ListRecentChangesTool : IAgentTool
{
    private readonly IChangeGroundingReader _changeReader;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ListRecentChangesTool> _logger;

    public ListRecentChangesTool(
        IChangeGroundingReader changeReader,
        IDateTimeProvider clock,
        ILogger<ListRecentChangesTool> logger)
    {
        _changeReader = changeReader;
        _clock = clock;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "list_recent_changes",
        "Lists recent production changes, deployments, and releases with their risk scores and status.",
        "change_intelligence",
        [
            new ToolParameterDefinition("service_name", "Filter by service name", "string"),
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
                "ListRecentChangesTool executing for service={Service}, env={Environment}, days={Days}",
                args.ServiceName, args.Environment, args.Days);

            var to = _clock.UtcNow;
            var from = to.AddDays(-args.Days);

            var releases = await _changeReader.FindRecentReleasesAsync(
                from,
                to,
                serviceId: args.ServiceName,
                environment: args.Environment ?? "production",
                tenantId: null,
                maxResults: args.Limit,
                ct: cancellationToken);

            var result = new
            {
                tool = "list_recent_changes",
                status = "success",
                filters = new
                {
                    service = args.ServiceName,
                    environment = args.Environment ?? "production",
                    days = args.Days,
                    limit = args.Limit,
                },
                total = releases.Count,
                changes = releases.Select(r => new
                {
                    releaseId = r.ReleaseId,
                    serviceName = r.ServiceName,
                    version = r.Version,
                    environment = r.Environment,
                    status = r.Status,
                    changeLevel = r.ChangeLevel,
                    changeScore = r.ChangeScore,
                    description = r.Description,
                    createdAt = r.CreatedAt,
                }),
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "list_recent_changes", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ListRecentChangesTool failed");
            return new ToolExecutionResult(
                false, "list_recent_changes", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static ListRecentChangesArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ListRecentChangesArgs(null, null, 7, 10);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var service = root.TryGetProperty("service_name", out var svcProp) ? svcProp.GetString() : null;
            var env = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var days = root.TryGetProperty("days", out var daysProp) && daysProp.TryGetInt32(out var d) ? d : 7;
            var limit = root.TryGetProperty("limit", out var limitProp) && limitProp.TryGetInt32(out var l) ? l : 10;

            return new ListRecentChangesArgs(service, env, days, limit);
        }
        catch
        {
            return new ListRecentChangesArgs(null, null, 7, 10);
        }
    }

    private sealed record ListRecentChangesArgs(string? ServiceName, string? Environment, int Days, int Limit);
}
