using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Repositório de regras de pipeline por tenant.
/// Permite CRUD e consulta filtrada por tipo de regra, sinal e estado.
/// </summary>
public interface ITenantPipelineRuleRepository
{
    /// <summary>Lista regras por tenant, ordenadas por Priority ascendente.</summary>
    Task<(IReadOnlyList<TenantPipelineRule> Items, int TotalCount)> ListAsync(
        PipelineRuleType? ruleType,
        PipelineSignalType? signalType,
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Lista regras activas por tenant e tipo de sinal, ordenadas por Priority (para o engine).</summary>
    Task<IReadOnlyList<TenantPipelineRule>> ListEnabledBySignalTypeAsync(
        PipelineSignalType signalType,
        CancellationToken ct);

    /// <summary>Obtém uma regra pelo identificador.</summary>
    Task<TenantPipelineRule?> GetByIdAsync(TenantPipelineRuleId id, CancellationToken ct);

    /// <summary>Adiciona uma nova regra.</summary>
    Task AddAsync(TenantPipelineRule rule, CancellationToken ct);

    /// <summary>Actualiza uma regra existente.</summary>
    Task UpdateAsync(TenantPipelineRule rule, CancellationToken ct);

    /// <summary>Remove uma regra.</summary>
    Task DeleteAsync(TenantPipelineRule rule, CancellationToken ct);
}
