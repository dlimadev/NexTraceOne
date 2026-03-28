namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Abstração de leitura de releases do módulo ChangeIntelligence.
/// Permite ao motor de correlação consultar mudanças sem acoplar a camada
/// de Application diretamente ao DbContext de outro módulo.
/// </summary>
public interface IChangeIntelligenceReader
{
    /// <summary>
    /// Retorna releases dentro de uma janela temporal, opcionalmente filtradas por ambiente e tenant.
    /// </summary>
    Task<IReadOnlyList<ChangeReleaseDto>> GetReleasesInWindowAsync(
        string? environment,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? tenantId,
        CancellationToken cancellationToken);
}

/// <summary>
/// DTO de leitura de uma release para uso exclusivo no motor de correlação.
/// Contém os campos mínimos necessários para scoring e persistência.
/// </summary>
public sealed record ChangeReleaseDto(
    Guid ReleaseId,
    Guid ApiAssetId,
    string ServiceName,
    string Environment,
    string? Description,
    DateTimeOffset CreatedAt,
    Guid? TenantId);
