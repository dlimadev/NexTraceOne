using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Repositório de regras de transformação log → métrica por tenant.
/// </summary>
public interface ILogToMetricRuleRepository
{
    /// <summary>Lista regras por tenant com paginação.</summary>
    Task<(IReadOnlyList<LogToMetricRule> Items, int TotalCount)> ListAsync(
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Lista regras activas para processamento.</summary>
    Task<IReadOnlyList<LogToMetricRule>> ListEnabledAsync(CancellationToken ct);

    /// <summary>Obtém uma regra pelo identificador.</summary>
    Task<LogToMetricRule?> GetByIdAsync(LogToMetricRuleId id, CancellationToken ct);

    /// <summary>Adiciona uma nova regra.</summary>
    Task AddAsync(LogToMetricRule rule, CancellationToken ct);

    /// <summary>Actualiza uma regra existente.</summary>
    Task UpdateAsync(LogToMetricRule rule, CancellationToken ct);

    /// <summary>Remove uma regra.</summary>
    Task DeleteAsync(LogToMetricRule rule, CancellationToken ct);
}
