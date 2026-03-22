using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório do ledger de consumo de tokens de IA.
/// Suporta registo, consulta por utilizador/tenant e cálculo de totais por período.
/// </summary>
public interface IAiTokenUsageLedgerRepository
{
    /// <summary>Adiciona uma nova entrada no ledger de consumo.</summary>
    Task AddAsync(AiTokenUsageLedger entity, CancellationToken ct = default);

    /// <summary>Lista entradas de consumo por utilizador, ordenadas por timestamp descendente.</summary>
    Task<IReadOnlyList<AiTokenUsageLedger>> GetByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Lista entradas de consumo por tenant, ordenadas por timestamp descendente.</summary>
    Task<IReadOnlyList<AiTokenUsageLedger>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Calcula o total de tokens consumidos por um utilizador num período específico.</summary>
    Task<long> GetTotalTokensForPeriodAsync(
        string userId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default);
}
