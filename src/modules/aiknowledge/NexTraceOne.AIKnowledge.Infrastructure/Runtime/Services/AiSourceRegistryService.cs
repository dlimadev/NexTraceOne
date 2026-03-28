using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do registo de fontes de dados para grounding e retrieval de IA.
/// Consulta o repositório de fontes e mapeia entidades de domínio para contratos de runtime.
/// Health check realiza verificação de conectividade HTTP para fontes do tipo Document
/// com URL configurada; outras fontes reportam o estado persistido.
/// </summary>
public sealed class AiSourceRegistryService : IAiSourceRegistryService
{
    private static readonly TimeSpan HttpHealthCheckTimeout = TimeSpan.FromSeconds(5);

    private readonly IAiSourceRepository _sourceRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiSourceRegistryService> _logger;

    public AiSourceRegistryService(
        IAiSourceRepository sourceRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<AiSourceRegistryService> logger)
    {
        _sourceRepository = sourceRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AiSourceInfo>> GetEnabledSourcesAsync(CancellationToken ct = default)
    {
        var sources = await _sourceRepository.GetEnabledAsync(ct);

        _logger.LogDebug("Retrieved {Count} enabled AI sources from registry", sources.Count);

        return sources.Select(MapToInfo).ToList();
    }

    public async Task<AiSourceInfo?> GetSourceByIdAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _sourceRepository.GetByIdAsync(AiSourceId.From(sourceId), ct);

        if (source is null)
        {
            _logger.LogDebug("AI source {SourceId} not found in registry", sourceId);
            return null;
        }

        return MapToInfo(source);
    }

    public async Task<AiSourceHealthResult> CheckSourceHealthAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _sourceRepository.GetByIdAsync(AiSourceId.From(sourceId), ct);

        if (source is null)
        {
            _logger.LogWarning("Cannot check health: AI source {SourceId} not found", sourceId);
            return new AiSourceHealthResult(sourceId, false, "Source not found");
        }

        var (isHealthy, message) = await PerformConnectivityCheckAsync(source, ct);
        var newStatus = isHealthy ? "Healthy" : "Unavailable";

        // Actualiza o estado de saúde persistido se mudou
        if (!string.Equals(source.HealthStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            source.UpdateHealth(newStatus);
            await _sourceRepository.UpdateAsync(source, ct);

            _logger.LogInformation(
                "AI source {SourceId} ({SourceName}) health status changed: {Old} → {New}",
                sourceId, source.Name, source.HealthStatus, newStatus);
        }
        else
        {
            _logger.LogDebug(
                "Health check for AI source {SourceId} ({SourceName}): {HealthStatus}",
                sourceId, source.Name, newStatus);
        }

        return new AiSourceHealthResult(sourceId, isHealthy, message);
    }

    /// <summary>
    /// Realiza verificação de conectividade dependendo do tipo da fonte.
    /// Document com URL HTTP/HTTPS: HEAD request com timeout de 5 s.
    /// Database, Telemetry, ExternalMemory: retorna estado persistido
    /// (conectores por tipo serão adicionados progressivamente).
    /// </summary>
    private async Task<(bool IsHealthy, string Message)> PerformConnectivityCheckAsync(
        AiSource source, CancellationToken ct)
    {
        if (source.SourceType == AiSourceType.Document
            && IsHttpUrl(source.ConnectionInfo))
        {
            return await CheckHttpConnectivityAsync(source.ConnectionInfo, ct);
        }

        // Para tipos sem conector HTTP, reportar o estado persistido
        var isHealthy = string.Equals(source.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase);
        return (isHealthy, source.HealthStatus);
    }

    private async Task<(bool IsHealthy, string Message)> CheckHttpConnectivityAsync(
        string url, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(HttpHealthCheckTimeout);

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            var isHealthy = response.IsSuccessStatusCode
                || (int)response.StatusCode < 500;

            var message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            _logger.LogDebug("HTTP health check for {Url}: {Status}", url, message);

            return (isHealthy, message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTTP health check for {Url} timed out after {Timeout}s", url, HttpHealthCheckTimeout.TotalSeconds);
            return (false, "Connection timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTTP health check for {Url} failed", url);
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private static bool IsHttpUrl(string connectionInfo)
        => connectionInfo.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           || connectionInfo.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static AiSourceInfo MapToInfo(AiSource source) =>
        new(
            Id: source.Id.Value,
            Name: source.Name,
            DisplayName: source.DisplayName,
            SourceType: source.SourceType.ToString(),
            Description: source.Description,
            IsEnabled: source.IsEnabled,
            Classification: source.Classification,
            OwnerTeam: source.OwnerTeam,
            HealthStatus: source.HealthStatus);
}

