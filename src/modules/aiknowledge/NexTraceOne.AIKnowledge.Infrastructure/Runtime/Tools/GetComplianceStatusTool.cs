using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: verifica o estado de compliance de um serviço para um standard (LGPD, GDPR, SOC2, ISO27001).
/// Usa ICatalogGroundingReader para dados do serviço; resposta de compliance é simulada
/// até que o módulo AuditCompliance exponha um grounding reader dedicado.
/// </summary>
public sealed class GetComplianceStatusTool : IAgentTool
{
    private readonly ICatalogGroundingReader _catalogReader;
    private readonly ILogger<GetComplianceStatusTool> _logger;

    public GetComplianceStatusTool(
        ICatalogGroundingReader catalogReader,
        ILogger<GetComplianceStatusTool> logger)
    {
        _catalogReader = catalogReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_compliance_status",
        "Checks compliance status of a service against a regulatory standard (LGPD, GDPR, SOC2, ISO27001).",
        "compliance",
        [
            new ToolParameterDefinition("serviceId", "Service identifier", "string", required: true),
            new ToolParameterDefinition("standard", "Compliance standard: LGPD | GDPR | SOC2 | ISO27001", "string"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (serviceId, standard) = ParseArgs(argumentsJson);

            _logger.LogInformation(
                "GetComplianceStatusTool: service={Service}, standard={Standard}",
                serviceId, standard);

            var services = await _catalogReader.FindServicesAsync(serviceId, serviceId, 3, cancellationToken);
            var service = services.FirstOrDefault();

            var hasRegulatoryScope = !string.IsNullOrWhiteSpace(service?.RegulatoryScope);
            var hasDataClassification = !string.IsNullOrWhiteSpace(service?.DataClassification);

            var result = new
            {
                tool = "get_compliance_status",
                serviceId,
                displayName = service?.DisplayName ?? serviceId,
                standard = standard ?? "All",
                regulatoryScope = service?.RegulatoryScope ?? "Not specified",
                dataClassification = service?.DataClassification ?? "Not specified",
                overallStatus = hasRegulatoryScope ? "Registered" : "Not assessed",
                simulatedNote = "Full compliance assessment requires AuditCompliance module grounding reader (planned AI-4.2 phase 2). Current data sourced from Service Catalog metadata only.",
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "get_compliance_status",
                JsonSerializer.Serialize(result),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetComplianceStatusTool failed");
            return new ToolExecutionResult(false, "get_compliance_status", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static (string serviceId, string? standard) ParseArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return ("", null);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var svc = root.TryGetProperty("serviceId", out var s) ? s.GetString() ?? "" : "";
            var std = root.TryGetProperty("standard", out var st) ? st.GetString() : null;
            return (svc, std);
        }
        catch { return ("", null); }
    }
}
