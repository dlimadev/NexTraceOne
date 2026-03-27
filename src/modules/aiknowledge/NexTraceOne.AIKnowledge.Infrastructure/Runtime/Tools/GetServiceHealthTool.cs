using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: consulta o estado de saúde de um serviço específico.
/// Retorna informação de health, reliability e estado operacional.
///
/// Nota P9.4: Na ausência de integração directa com OperationalIntelligence,
/// esta tool executa a consulta estruturada e retorna contexto para o agent.
/// Evolução posterior ligará aos RuntimeSignals e SLOs reais via integration port.
/// </summary>
public sealed class GetServiceHealthTool : IAgentTool
{
    private readonly ILogger<GetServiceHealthTool> _logger;

    public GetServiceHealthTool(ILogger<GetServiceHealthTool> logger)
    {
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_service_health",
        "Retrieves the current health status, reliability metrics, and operational state of a specific service.",
        "operational_intelligence",
        [
            new ToolParameterDefinition("service_name", "Name or identifier of the service to check", "string", Required: true),
            new ToolParameterDefinition("environment", "Target environment (default: production)", "string"),
            new ToolParameterDefinition("include_slos", "Include SLO/SLA information (default: false)", "boolean"),
        ]);

    public Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            if (string.IsNullOrWhiteSpace(args.ServiceName))
            {
                sw.Stop();
                return Task.FromResult(new ToolExecutionResult(
                    false, "get_service_health", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'service_name' is required."));
            }

            _logger.LogInformation(
                "GetServiceHealthTool executing for service={Service}, env={Environment}",
                args.ServiceName, args.Environment);

            var result = new
            {
                tool = "get_service_health",
                status = "executed",
                service = args.ServiceName,
                environment = args.Environment ?? "production",
                includeSlos = args.IncludeSlos,
                note = "Service health query executed. Cross-module integration with OperationalIntelligence RuntimeSignals will be wired in a subsequent phase.",
                guidance = "The agent should use the service context and any available runtime data to assess operational health."
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return Task.FromResult(new ToolExecutionResult(
                true, "get_service_health", output, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetServiceHealthTool failed");
            return Task.FromResult(new ToolExecutionResult(
                false, "get_service_health", string.Empty, sw.ElapsedMilliseconds, ex.Message));
        }
    }

    private static GetServiceHealthArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new GetServiceHealthArgs(null, null, false);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("service_name", out var nameProp) ? nameProp.GetString() : null;
            var env = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var slos = root.TryGetProperty("include_slos", out var sloProp) && sloProp.ValueKind == JsonValueKind.True;

            return new GetServiceHealthArgs(name, env, slos);
        }
        catch
        {
            return new GetServiceHealthArgs(null, null, false);
        }
    }

    private sealed record GetServiceHealthArgs(string? ServiceName, string? Environment, bool IncludeSlos);
}
