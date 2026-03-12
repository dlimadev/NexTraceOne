namespace NexTraceOne.ChangeIntelligence.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo ChangeIntelligence.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IChangeIntelligenceModule
{
    /// <summary>Obtém os dados de uma release por ID.</summary>
    Task<ReleaseDto?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken);

    /// <summary>Obtém o score de mudança de uma release.</summary>
    Task<decimal?> GetChangeScoreAsync(Guid releaseId, CancellationToken cancellationToken);

    /// <summary>Obtém o relatório de blast radius de uma release.</summary>
    Task<BlastRadiusDto?> GetBlastRadiusAsync(Guid releaseId, CancellationToken cancellationToken);
}

/// <summary>DTO de release para comunicação entre módulos.</summary>
public sealed record ReleaseDto(
    Guid ReleaseId,
    Guid ApiAssetId,
    string ServiceName,
    string Version,
    string Environment,
    string Status,
    string ChangeLevel,
    DateTimeOffset CreatedAt);

/// <summary>DTO de blast radius para comunicação entre módulos.</summary>
public sealed record BlastRadiusDto(
    Guid ReportId,
    Guid ReleaseId,
    int TotalAffectedConsumers,
    IReadOnlyList<string> DirectConsumers,
    IReadOnlyList<string> TransitiveConsumers,
    DateTimeOffset CalculatedAt);
