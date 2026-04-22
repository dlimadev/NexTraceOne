using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.HealthChecks;

/// <summary>
/// Health check para o ClickHouse Analytics.
/// Executa SELECT 1 via HTTP API do ClickHouse e reporta Healthy ou Unhealthy.
/// Registado automaticamente quando Analytics:Enabled = true e Provider = "ClickHouse".
/// </summary>
public sealed class ClickHouseHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<ClickHouseHealthCheck> _logger;

    public ClickHouseHealthCheck(
        HttpClient httpClient,
        IOptions<AnalyticsOptions> options,
        ILogger<ClickHouseHealthCheck> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint();
            var content = new StringContent("SELECT 1 FORMAT JSONEachRow", Encoding.UTF8, "text/plain");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.PostAsync(endpoint, content, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("ClickHouse is reachable.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "ClickHouseHealthCheck: SELECT 1 failed with status {Status}. Body: {Body}",
                (int)response.StatusCode, body);

            return HealthCheckResult.Unhealthy(
                $"ClickHouse responded with HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("ClickHouse health check timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouseHealthCheck: Exception during health check.");
            return HealthCheckResult.Unhealthy($"ClickHouse unreachable: {ex.Message}");
        }
    }

    private string BuildEndpoint()
    {
        var baseUrl = _options.ConnectionString.TrimEnd('/');
        return baseUrl.Contains('?') ? baseUrl : $"{baseUrl}/?";
    }
}
