using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Bridge real entre IRuntimeComparisonReader (ChangeGovernance) e o módulo
/// OperationalIntelligence. Usa IRuntimeIntelligenceModule (contrato público OI) para obter
/// métricas de runtime de cada ambiente e calcula deltas comparativos.
/// Registada na composition root (ApiHost) substituindo NullRuntimeComparisonReader.
/// Preserva a fronteira do bounded context — sem referência a OI.Application.
/// </summary>
public sealed class OtelRuntimeComparisonReader(IRuntimeIntelligenceModule runtimeModule) : IRuntimeComparisonReader
{
    public async Task<RuntimeComparisonSnapshot> CompareAsync(
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

        try
        {
            var fromMetrics = await runtimeModule.GetServiceMetricsAsync(serviceName, environmentFrom, cancellationToken);
            var toMetrics = await runtimeModule.GetServiceMetricsAsync(serviceName, environmentTo, cancellationToken);

            if (fromMetrics is null || toMetrics is null)
            {
                return new RuntimeComparisonSnapshot(
                    ServiceName: serviceName,
                    EnvironmentFrom: environmentFrom,
                    EnvironmentTo: environmentTo,
                    WindowDays: windowDays,
                    ErrorRateDelta: null,
                    LatencyP95DeltaMs: null,
                    ThroughputDelta: null,
                    CostDelta: null,
                    IncidentsDelta: null,
                    DataQuality: 0.2m,
                    SimulatedNote: "Insufficient runtime data for one or both environments.");
            }

            var errorRateDelta = toMetrics.ErrorRate - fromMetrics.ErrorRate;
            var latencyDelta = (decimal)(toMetrics.AverageLatencyMs - fromMetrics.AverageLatencyMs);

            // Data quality scales with sample count
            var minSamples = Math.Min(fromMetrics.SampleCount, toMetrics.SampleCount);
            var dataQuality = Math.Min(1m, minSamples / 10m);

            return new RuntimeComparisonSnapshot(
                ServiceName: serviceName,
                EnvironmentFrom: environmentFrom,
                EnvironmentTo: environmentTo,
                WindowDays: windowDays,
                ErrorRateDelta: errorRateDelta,
                LatencyP95DeltaMs: latencyDelta,
                ThroughputDelta: null,
                CostDelta: null,
                IncidentsDelta: null,
                DataQuality: dataQuality,
                SimulatedNote: null);
        }
        catch (Exception ex)
        {
            return new RuntimeComparisonSnapshot(
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
                SimulatedNote: $"Runtime comparison unavailable: {ex.Message}");
        }
    }
}
