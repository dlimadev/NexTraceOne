using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.CommercialCatalog.Application.Abstractions;

/// <summary>
/// Repositório de planos comerciais do subdomínio CommercialCatalog.
/// Operações de leitura e escrita para o aggregate Plan.
/// </summary>
public interface IPlanRepository
{
    /// <summary>Obtém um plano pelo identificador.</summary>
    Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default);

    /// <summary>Obtém um plano pelo código único.</summary>
    Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista planos com filtro opcional por estado ativo.
    /// Se activeOnly for null, retorna todos os planos.
    /// </summary>
    Task<IReadOnlyList<Plan>> ListAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo plano para persistência.</summary>
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
}
