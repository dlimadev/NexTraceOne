using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Contrato de repositório para o estado do seed de demonstração.</summary>
public interface IDemoSeedStateRepository
{
    /// <summary>Obtém ou cria o estado do seed para o tenant actual.</summary>
    Task<DemoSeedState> GetOrCreateAsync(Guid? tenantId, DateTimeOffset now, CancellationToken ct);

    /// <summary>Atualiza o estado do seed.</summary>
    void Update(DemoSeedState state);
}
