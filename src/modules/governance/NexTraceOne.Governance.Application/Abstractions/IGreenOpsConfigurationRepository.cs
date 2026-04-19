using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Contrato de repositório para configuração GreenOps por tenant.</summary>
public interface IGreenOpsConfigurationRepository
{
    /// <summary>
    /// Obtém a configuração GreenOps activa para um tenant.
    /// Retorna null se nenhuma configuração foi persistida.
    /// </summary>
    Task<GreenOpsConfiguration?> GetActiveAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Adiciona uma nova configuração GreenOps.</summary>
    Task AddAsync(GreenOpsConfiguration config, CancellationToken ct);

    /// <summary>Atualiza a configuração GreenOps existente.</summary>
    void Update(GreenOpsConfiguration config);
}
