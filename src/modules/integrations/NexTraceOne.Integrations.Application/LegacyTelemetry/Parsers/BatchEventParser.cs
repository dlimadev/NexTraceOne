using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;

/// <summary>
/// Parser para eventos de execução batch mainframe.
/// Normaliza BatchEventRequest em NormalizedLegacyEvent canónico.
/// </summary>
public sealed class BatchEventParser : ILegacyEventParser<BatchEventRequest>
{
    public NormalizedLegacyEvent Parse(BatchEventRequest request)
    {
        var severity = DetermineBatchSeverity(request.Status, request.ReturnCode);
        var attributes = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Provider)) attributes["provider"] = request.Provider;
        if (!string.IsNullOrWhiteSpace(request.JobId)) attributes["job_id"] = request.JobId;
        if (!string.IsNullOrWhiteSpace(request.StepName)) attributes["step_name"] = request.StepName;
        if (!string.IsNullOrWhiteSpace(request.ProgramName)) attributes["program_name"] = request.ProgramName;
        if (!string.IsNullOrWhiteSpace(request.ReturnCode)) attributes["return_code"] = request.ReturnCode;
        if (!string.IsNullOrWhiteSpace(request.Status)) attributes["status"] = request.Status;
        if (!string.IsNullOrWhiteSpace(request.ChainName)) attributes["chain_name"] = request.ChainName;
        if (request.StartedAt.HasValue) attributes["started_at"] = request.StartedAt.Value.ToString("O");
        if (request.CompletedAt.HasValue) attributes["completed_at"] = request.CompletedAt.Value.ToString("O");
        if (request.DurationMs.HasValue) attributes["duration_ms"] = request.DurationMs.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) attributes["correlation_id"] = request.CorrelationId;

        if (request.Metadata is not null)
        {
            foreach (var kvp in request.Metadata)
                attributes.TryAdd(kvp.Key, kvp.Value);
        }

        var message = BuildBatchMessage(request);

        return new NormalizedLegacyEvent(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: "batch_execution",
            SourceType: LegacyEventSourceType.Batch,
            SystemName: request.SystemName,
            LparName: request.LparName,
            ServiceName: request.JobName,
            AssetName: request.ProgramName ?? request.JobName,
            Severity: severity,
            Message: message,
            Timestamp: request.CompletedAt ?? request.StartedAt ?? DateTimeOffset.UtcNow,
            Attributes: attributes);
    }

    private static string DetermineBatchSeverity(string? status, string? returnCode)
    {
        if (string.Equals(status, "abended", StringComparison.OrdinalIgnoreCase) ||
            (returnCode is not null && returnCode.StartsWith("ABEND", StringComparison.OrdinalIgnoreCase)))
            return LegacySeverity.Critical;

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            return LegacySeverity.Error;

        if (returnCode is not null && returnCode != "0000" && returnCode != "0004" && returnCode != "0")
            return LegacySeverity.Warning;

        return LegacySeverity.Info;
    }

    private static string BuildBatchMessage(BatchEventRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.JobName)) parts.Add($"Job={request.JobName}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"Status={request.Status}");
        if (!string.IsNullOrWhiteSpace(request.ReturnCode)) parts.Add($"RC={request.ReturnCode}");
        if (request.DurationMs.HasValue) parts.Add($"Duration={request.DurationMs}ms");
        return parts.Count > 0 ? string.Join(", ", parts) : "Batch event";
    }
}
