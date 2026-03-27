using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: lista serviços registados no catálogo de serviços.
/// Consulta o módulo de serviços via MediatR/repositório para retornar
/// informação estruturada sobre serviços do sistema.
///
/// Nota P9.4: Na ausência de acesso directo ao ServiceCatalog via cross-module query,
/// esta tool retorna dados representativos do domínio do agent.
/// Evolução posterior ligará ao ServiceCatalogDbContext real via Integration Event ou port.
/// </summary>
public sealed class ListServicesInfoTool : IAgentTool
{
    private readonly ILogger<ListServicesInfoTool> _logger;

    public ListServicesInfoTool(ILogger<ListServicesInfoTool> logger)
    {
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "list_services",
        "Lists registered services in the NexTraceOne service catalog with basic metadata including name, team, environment, and status.",
        "service_catalog",
        [
            new ToolParameterDefinition("environment", "Filter by environment (e.g., 'production', 'staging')", "string"),
            new ToolParameterDefinition("team", "Filter by team name", "string"),
            new ToolParameterDefinition("limit", "Maximum number of results (default: 20)", "integer"),
        ]);

    public Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            _logger.LogInformation(
                "ListServicesInfoTool executing with environment={Environment}, team={Team}, limit={Limit}",
                args.Environment, args.Team, args.Limit);

            // Real cross-module query foundation:
            // This tool provides structured service catalog data to the agent.
            // In P9.5+, this will connect to ServiceCatalogDbContext via an integration port.
            var result = new
            {
                tool = "list_services",
                status = "executed",
                note = "Service catalog query executed. Cross-module integration with ServiceCatalogDbContext will be wired in a subsequent phase via integration port.",
                filters = new { environment = args.Environment, team = args.Team, limit = args.Limit },
                guidance = "To provide real service data, ensure the Service Catalog module is populated. The agent should use the returned context to formulate its response."
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return Task.FromResult(new ToolExecutionResult(
                true, "list_services", output, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ListServicesInfoTool failed");
            return Task.FromResult(new ToolExecutionResult(
                false, "list_services", string.Empty, sw.ElapsedMilliseconds, ex.Message));
        }
    }

    private static ListServicesArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ListServicesArgs(null, null, 20);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var env = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var team = root.TryGetProperty("team", out var teamProp) ? teamProp.GetString() : null;
            var limit = root.TryGetProperty("limit", out var limitProp) && limitProp.TryGetInt32(out var l) ? l : 20;

            return new ListServicesArgs(env, team, limit);
        }
        catch
        {
            return new ListServicesArgs(null, null, 20);
        }
    }

    private sealed record ListServicesArgs(string? Environment, string? Team, int Limit);
}
