namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Superfície read-only de comparação runtime entre ambientes para uso em Promotion Readiness.
///
/// Esta interface é owned por ChangeGovernance porque a decisão de promoção
/// é uma decisão de governança de mudança. A implementação real depende de
/// dados de OperationalIntelligence (primitiva <c>CompareEnvironments</c>) e
/// é ligada na composition root via bridge — mantendo a fronteira do bounded
/// context e evitando referência direta entre módulos.
///
/// O default registado em <c>ChangeGovernance.Infrastructure</c> é o
/// <c>NullRuntimeComparisonReader</c>, que devolve um snapshot simulado e
/// marcado com <see cref="RuntimeComparisonSnapshot.SimulatedNote"/> para
/// que a UI possa sinalizar o estado.
///
/// Toda consulta é tenant-aware por design.
/// </summary>
public interface IRuntimeComparisonReader
{
    /// <summary>
    /// Compara o comportamento runtime de um serviço entre dois ambientes
    /// numa janela de observação e devolve deltas relativos prontos para
    /// alimentar um gate de promoção ou a UX do ReleaseTrain.
    /// </summary>
    /// <param name="tenantId">Tenant alvo.</param>
    /// <param name="serviceName">Nome canónico do serviço.</param>
    /// <param name="environmentFrom">Ambiente de origem (ex.: "Staging").</param>
    /// <param name="environmentTo">Ambiente de destino (ex.: "Production").</param>
    /// <param name="windowDays">Janela de observação, em dias.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<RuntimeComparisonSnapshot> CompareAsync(
        Guid tenantId,
        string serviceName,
        string environmentFrom,
        string environmentTo,
        int windowDays,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot comparativo entre dois ambientes. Todos os deltas são relativos
/// (fração), sendo negativos quando o ambiente de destino tem comportamento
/// pior que o de origem.
/// </summary>
/// <param name="ServiceName">Serviço comparado.</param>
/// <param name="EnvironmentFrom">Ambiente de origem.</param>
/// <param name="EnvironmentTo">Ambiente de destino.</param>
/// <param name="WindowDays">Janela de observação.</param>
/// <param name="ErrorRateDelta">Δ da taxa de erro (To - From), fração; null quando sem dados.</param>
/// <param name="LatencyP95DeltaMs">Δ do p95 de latência em ms (To - From); null quando sem dados.</param>
/// <param name="ThroughputDelta">Δ relativo de throughput ((To - From)/From); null quando sem dados.</param>
/// <param name="CostDelta">Δ relativo de custo ((To - From)/From); null quando sem dados.</param>
/// <param name="IncidentsDelta">Δ do nº de incidentes (To - From); null quando sem dados.</param>
/// <param name="DataQuality">Qualidade dos dados: 0..1. 0 quando totalmente simulado.</param>
/// <param name="SimulatedNote">Nota não-null quando o snapshot é simulado (default Null reader).</param>
public sealed record RuntimeComparisonSnapshot(
    string ServiceName,
    string EnvironmentFrom,
    string EnvironmentTo,
    int WindowDays,
    decimal? ErrorRateDelta,
    decimal? LatencyP95DeltaMs,
    decimal? ThroughputDelta,
    decimal? CostDelta,
    int? IncidentsDelta,
    decimal DataQuality,
    string? SimulatedNote);
