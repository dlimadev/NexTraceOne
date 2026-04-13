using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: lista versões de um contrato ou serviço através do histórico de releases.
/// Consulta o módulo ChangeIntelligence via IChangeGroundingReader para retornar
/// o histórico de versões implantadas, com ambiente, estado e nível de mudança.
/// Útil para agents de contract governance, change analysis e release management.
/// </summary>
public sealed class ListContractVersionsTool : IAgentTool
{
    private readonly IChangeGroundingReader _changeReader;
    private readonly ILogger<ListContractVersionsTool> _logger;

    public ListContractVersionsTool(
        IChangeGroundingReader changeReader,
        ILogger<ListContractVersionsTool> logger)
    {
        _changeReader = changeReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "list_contract_versions",
        "Lists the version history of a contract or service by querying recent releases. Returns versions deployed per environment with change level and status.",
        "contract_governance",
        [
            new ToolParameterDefinition("serviceId", "Service or contract identifier (required)", "string", Required: true),
            new ToolParameterDefinition("environment", "Filter by environment (e.g. production, staging). Omit for all environments.", "string"),
            new ToolParameterDefinition("days", "Number of days to look back for releases (default: 90)", "integer"),
            new ToolParameterDefinition("limit", "Maximum number of versions to return (default: 10)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            if (string.IsNullOrWhiteSpace(args.ServiceId))
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "list_contract_versions", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'serviceId' is required.");
            }

            var limit = Math.Clamp(args.Limit, 1, 50);
            var days = Math.Clamp(args.Days, 1, 365);
            var from = DateTimeOffset.UtcNow.AddDays(-days);
            var to = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "ListContractVersionsTool executing for serviceId={ServiceId}, environment={Environment}, days={Days}",
                args.ServiceId, args.Environment, days);

            var releases = await _changeReader.FindRecentReleasesAsync(
                from: from,
                to: to,
                serviceId: args.ServiceId,
                environment: args.Environment,
                tenantId: null,
                maxResults: limit,
                ct: cancellationToken);

            var result = new
            {
                tool = "list_contract_versions",
                status = "executed",
                serviceId = args.ServiceId,
                environment = args.Environment ?? "all",
                periodDays = days,
                total = releases.Count,
                versions = releases.Select(r => new
                {
                    releaseId = r.ReleaseId,
                    version = r.Version,
                    environment = r.Environment,
                    status = r.Status,
                    changeLevel = r.ChangeLevel,
                    changeScore = r.ChangeScore,
                    description = r.Description,
                    releasedAt = r.CreatedAt,
                }),
                guidance = releases.Count == 0
                    ? $"No releases found for '{args.ServiceId}' in the last {days} days. Check the Change Intelligence module for the full history."
                    : null,
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "list_contract_versions", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ListContractVersionsTool failed");
            return new ToolExecutionResult(
                false, "list_contract_versions", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static ListContractVersionsArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ListContractVersionsArgs(null, null, 90, 10);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var serviceId = root.TryGetProperty("serviceId", out var idProp) ? idProp.GetString() : null;
            var env = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : null;
            var days = root.TryGetProperty("days", out var daysProp) && daysProp.TryGetInt32(out var d) ? d : 90;
            var limit = root.TryGetProperty("limit", out var limProp) && limProp.TryGetInt32(out var l) ? l : 10;

            return new ListContractVersionsArgs(serviceId, env, days, limit);
        }
        catch
        {
            return new ListContractVersionsArgs(null, null, 90, 10);
        }
    }

    private sealed record ListContractVersionsArgs(
        string? ServiceId,
        string? Environment,
        int Days,
        int Limit);
}
