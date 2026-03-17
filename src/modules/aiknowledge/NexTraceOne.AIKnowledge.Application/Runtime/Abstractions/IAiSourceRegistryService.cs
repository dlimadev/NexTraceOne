namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de registo e consulta de fontes de dados para grounding e retrieval de IA.
/// Centraliza o acesso às fontes registadas, mapeando entidades de domínio para contratos de runtime.
/// </summary>
public interface IAiSourceRegistryService
{
    /// <summary>Lista todas as fontes ativas registadas na plataforma.</summary>
    Task<IReadOnlyList<AiSourceInfo>> GetEnabledSourcesAsync(CancellationToken ct = default);

    /// <summary>Obtém uma fonte específica pelo identificador.</summary>
    Task<AiSourceInfo?> GetSourceByIdAsync(Guid sourceId, CancellationToken ct = default);

    /// <summary>Verifica a saúde de uma fonte específica.</summary>
    Task<AiSourceHealthResult> CheckSourceHealthAsync(Guid sourceId, CancellationToken ct = default);
}

/// <summary>Informação de runtime sobre uma fonte de dados de IA.</summary>
public sealed record AiSourceInfo(
    Guid Id,
    string Name,
    string DisplayName,
    string SourceType,
    string Description,
    bool IsEnabled,
    string Classification,
    string OwnerTeam,
    string HealthStatus);

/// <summary>Resultado do health check de uma fonte de dados.</summary>
public sealed record AiSourceHealthResult(
    Guid SourceId,
    bool IsHealthy,
    string? Message = null);
