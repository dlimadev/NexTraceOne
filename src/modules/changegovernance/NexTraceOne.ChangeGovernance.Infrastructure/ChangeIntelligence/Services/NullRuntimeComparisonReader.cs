using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação default (honest-null) de <see cref="IRuntimeComparisonReader"/>.
///
/// Devolve um snapshot simulado e marcado com <see cref="RuntimeComparisonSnapshot.SimulatedNote"/>
/// para que a UI sinalize claramente que não há dados reais. A ligação com
/// OperationalIntelligence (comparação real entre ambientes) é feita por uma bridge
/// dedicada na composition root e, quando registada, substitui este default.
///
/// Esta abordagem preserva a fronteira do bounded context e permite o produto
/// funcionar em instalações self-hosted antes da bridge real estar configurada.
/// </summary>
internal sealed class NullRuntimeComparisonReader : IRuntimeComparisonReader
{
    /// <inheritdoc />
    public Task<RuntimeComparisonSnapshot> CompareAsync(
        Guid tenantId,
        string serviceName,
        string environmentFrom,
        string environmentTo,
        int windowDays,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentFrom);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentTo);

        var snapshot = new RuntimeComparisonSnapshot(
            ServiceName: serviceName,
            EnvironmentFrom: environmentFrom,
            EnvironmentTo: environmentTo,
            WindowDays: windowDays,
            ErrorRateDelta: null,
            LatencyP95DeltaMs: null,
            ThroughputDelta: null,
            CostDelta: null,
            IncidentsDelta: null,
            DataQuality: 0m,
            SimulatedNote: "No runtime comparison bridge configured; Promotion Readiness Delta is running in simulated mode.");

        return Task.FromResult(snapshot);
    }
}
