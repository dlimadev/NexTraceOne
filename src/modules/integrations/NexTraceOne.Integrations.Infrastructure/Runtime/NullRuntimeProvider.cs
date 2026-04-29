using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Runtime;

/// <summary>
/// Implementação nula de IRuntimeProvider.
/// Retorna matriz vazia enquanto nenhum agente de runtime (CLR profiler, eBPF) estiver configurado.
/// </summary>
internal sealed class NullRuntimeProvider : IRuntimeProvider
{
    public bool IsConfigured => false;

    public Task<IReadOnlyList<RuntimeModuleInfo>> GetModuleMatrixAsync(
        string? tenantId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RuntimeModuleInfo>>([]);
}
