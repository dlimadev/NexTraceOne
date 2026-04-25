using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: verifica o estado de SLO de um serviço via IServiceInterfaceGroundingReader.
/// Retorna targets de SLO e indica se estão a ser cumpridos com base em contexto disponível.
/// </summary>
public sealed class CheckSloStatusTool : IAgentTool
{
    private readonly IServiceInterfaceGroundingReader _interfaceReader;
    private readonly ICatalogGroundingReader _catalogReader;
    private readonly ILogger<CheckSloStatusTool> _logger;

    public CheckSloStatusTool(
        IServiceInterfaceGroundingReader interfaceReader,
        ICatalogGroundingReader catalogReader,
        ILogger<CheckSloStatusTool> logger)
    {
        _interfaceReader = interfaceReader;
        _catalogReader = catalogReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "check_slo_status",
        "Checks the SLO targets and interface compliance status for a service.",
        "service_catalog",
        [
            new ToolParameterDefinition("serviceId", "Service name or identifier", "string", required: true),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var serviceId = ParseServiceId(argumentsJson);

            _logger.LogInformation("CheckSloStatusTool: service={Service}", serviceId);

            var interfacesTask = _interfaceReader.FindInterfacesByServiceAsync(serviceId, 20, cancellationToken);
            var servicesTask = _catalogReader.FindServicesAsync(serviceId, serviceId, 5, cancellationToken);

            await Task.WhenAll(interfacesTask, servicesTask);

            var interfaces = interfacesTask.Result;
            var services = servicesTask.Result;
            var service = services.FirstOrDefault();

            var sloInterfaces = interfaces
                .Where(i => !string.IsNullOrWhiteSpace(i.SloTarget))
                .ToList();

            var result = new
            {
                tool = "check_slo_status",
                serviceId,
                displayName = service?.DisplayName ?? serviceId,
                criticality = service?.Criticality ?? "Unknown",
                serviceLevelObjective = service?.SloTarget ?? "Not defined",
                interfaceCount = interfaces.Count,
                interfacesWithSlo = sloInterfaces.Count,
                sloTargets = sloInterfaces.Select(i => new
                {
                    interfaceName = i.Name,
                    interfaceType = i.InterfaceType,
                    sloTarget = i.SloTarget,
                    status = i.Status,
                    exposureScope = i.ExposureScope,
                    deprecationDate = i.DeprecationDate,
                }),
                note = "SLO compliance data requires operational metrics integration (AI-4.3)",
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "check_slo_status",
                JsonSerializer.Serialize(result),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "CheckSloStatusTool failed");
            return new ToolExecutionResult(false, "check_slo_status", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static string ParseServiceId(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("serviceId", out var p) ? p.GetString() ?? "" : "";
        }
        catch { return ""; }
    }
}
