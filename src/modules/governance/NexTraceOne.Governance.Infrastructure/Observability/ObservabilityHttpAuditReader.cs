using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Observability;

/// <summary>
/// Implementação do IHttpAuditReader que delega para o IObservabilityProvider.
/// Consulta traces de tipo HTTP/REST para construir o registo de auditoria de chamadas externas.
/// Usa fallback gracioso para lista vazia quando o provider não está disponível ou não está configurado.
/// </summary>
internal sealed class ObservabilityHttpAuditReader(
    IObservabilityProvider observabilityProvider,
    ILogger<ObservabilityHttpAuditReader> logger) : IHttpAuditReader
{
    /// <inheritdoc />
    public async Task<HttpAuditPage> QueryAsync(HttpAuditFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await observabilityProvider.IsHealthyAsync(cancellationToken);
            if (!isHealthy)
                return new HttpAuditPage([], 0, IsLiveData: false);

            var from = filter.From ?? DateTimeOffset.UtcNow.AddHours(-24);
            var to = filter.To ?? DateTimeOffset.UtcNow;
            var fetchLimit = Math.Min(filter.Page * filter.PageSize + filter.PageSize, 1000);

            var traceFilter = new TraceQueryFilter
            {
                Environment = filter.Context ?? "production",
                From = from,
                Until = to,
                ServiceKind = ServiceKindValues.Rest,
                Limit = fetchLimit
            };

            var traces = await observabilityProvider.QueryTracesAsync(traceFilter, cancellationToken);

            // Apply destination filter if provided
            IEnumerable<TraceSummary> filtered = traces;
            if (!string.IsNullOrWhiteSpace(filter.Destination))
            {
                filtered = filtered.Where(t =>
                    t.OperationName.Contains(filter.Destination, StringComparison.OrdinalIgnoreCase));
            }

            var all = filtered.ToList();
            var total = all.Count;

            var skip = (filter.Page - 1) * filter.PageSize;
            var page = all
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(t => new HttpAuditEntry(
                    Id: t.TraceId,
                    Destination: t.OperationName,
                    Method: ExtractHttpMethod(t.OperationName),
                    StatusCode: t.HasErrors ? 500 : ParseStatusCode(t.StatusCode),
                    DurationMs: (long)t.DurationMs,
                    Context: t.ServiceName,
                    OccurredAt: t.StartTime))
                .ToList();

            return new HttpAuditPage(page, total, IsLiveData: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to query HTTP audit data from observability provider. Returning empty page.");
            return new HttpAuditPage([], 0, IsLiveData: false);
        }
    }

    private static string ExtractHttpMethod(string operationName)
    {
        // OpenTelemetry semantic convention: "HTTP GET /path" or "GET /path"
        if (string.IsNullOrWhiteSpace(operationName))
            return "HTTP";

        var parts = operationName.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length >= 1 && IsHttpMethod(parts[0]))
            return parts[0];

        return "HTTP";
    }

    private static int ParseStatusCode(string? statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return 200;

        // OTel status: "STATUS_CODE_OK", "STATUS_CODE_ERROR", or numeric string
        if (int.TryParse(statusCode, out var code))
            return code;

        return statusCode.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ? 500 : 200;
    }

    private static bool IsHttpMethod(string value) =>
        value is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS";
}
