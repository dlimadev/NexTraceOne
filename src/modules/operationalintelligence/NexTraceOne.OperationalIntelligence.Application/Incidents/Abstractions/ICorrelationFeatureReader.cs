namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Abstração para leitura de detalhes de uma mudança (change/release) a partir
/// do módulo ChangeGovernance. Permite ao motor de correlação obter informações
/// enriquecidas sem acoplamento direto ao módulo externo.
/// </summary>
public interface ICorrelationFeatureReader
{
    /// <summary>
    /// Retorna os detalhes de uma release/change para cálculo de feature scores de correlação.
    /// Retorna null se a mudança não for encontrada ou não estiver acessível.
    /// </summary>
    Task<ChangeReleaseDetails?> GetChangeDetailsAsync(Guid changeId, CancellationToken ct);
}

/// <summary>
/// Detalhe de uma release/change para uso no motor de correlação feature-based.
/// </summary>
public sealed record ChangeReleaseDetails(
    Guid ChangeId,
    string ServiceName,
    string OwnerTeam,
    DateTimeOffset DeployedAt,
    string Environment);
