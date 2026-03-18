using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do registo de fontes de dados para grounding e retrieval de IA.
/// Consulta o repositório de fontes e mapeia entidades de domínio para contratos de runtime.
/// Health check é stub — será implementado quando houver conectores reais por tipo de fonte.
/// </summary>
public sealed class AiSourceRegistryService : IAiSourceRegistryService
{
    private readonly IAiSourceRepository _sourceRepository;
    private readonly ILogger<AiSourceRegistryService> _logger;

    public AiSourceRegistryService(
        IAiSourceRepository sourceRepository,
        ILogger<AiSourceRegistryService> logger)
    {
        _sourceRepository = sourceRepository;
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

        // Stub: retorna o health status armazenado na entidade.
        // Implementação real virá com conectores por tipo de fonte (HTTP, DB, file system, etc.).
        var isHealthy = string.Equals(source.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug(
            "Health check for AI source {SourceId} ({SourceName}): {HealthStatus}",
            sourceId, source.Name, source.HealthStatus);

        return new AiSourceHealthResult(sourceId, isHealthy, source.HealthStatus);
    }

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
