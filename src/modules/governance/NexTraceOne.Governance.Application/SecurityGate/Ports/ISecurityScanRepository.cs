using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Ports;

/// <summary>
/// Porta de repositório para SecurityScanResult e SecurityFinding.
/// </summary>
public interface ISecurityScanRepository
{
    /// <summary>Retorna um resultado de scan pelo identificador.</summary>
    Task<SecurityScanResult?> FindByIdAsync(Guid scanId, CancellationToken ct);

    /// <summary>Lista resultados de scan com achados filtrados por severidade e categoria.</summary>
    Task<IReadOnlyList<SecurityFinding>> ListFindingsAsync(
        Guid? targetId,
        FindingSeverity minSeverity,
        SecurityCategory? category,
        FindingStatus? status,
        int pageSize,
        int pageNumber,
        CancellationToken ct);

    /// <summary>Conta total de scans e scans que passaram o gate.</summary>
    Task<(int TotalScans, int PassedScans)> GetScanCountsAsync(CancellationToken ct);

    /// <summary>Retorna distribuição de achados por categoria (top N).</summary>
    Task<IReadOnlyList<(string Category, int Count)>> GetTopCategoriesAsync(int topN, CancellationToken ct);

    /// <summary>Adiciona um novo resultado de scan.</summary>
    Task AddAsync(SecurityScanResult scanResult, CancellationToken ct);

    /// <summary>Atualiza um resultado de scan existente.</summary>
    Task UpdateAsync(SecurityScanResult scanResult, CancellationToken ct);
}
