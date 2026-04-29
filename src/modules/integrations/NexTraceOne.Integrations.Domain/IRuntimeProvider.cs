namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com agentes de runtime intelligence (CLR profiler, eBPF, OpenTelemetry runtime metrics).
/// A implementação padrão é <c>NullRuntimeProvider</c> que retorna matriz vazia até que um agente real esteja configurado.
/// DEG-03 — Runtime Intelligence.
/// </summary>
public interface IRuntimeProvider
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<RuntimeModuleInfo>> GetModuleMatrixAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Informação de um módulo de runtime detectado pelo agente.</summary>
public sealed record RuntimeModuleInfo(
    string ModuleName,
    string Version,
    string Status,
    double CpuPct,
    long MemoryBytes,
    DateTimeOffset DetectedAt);
