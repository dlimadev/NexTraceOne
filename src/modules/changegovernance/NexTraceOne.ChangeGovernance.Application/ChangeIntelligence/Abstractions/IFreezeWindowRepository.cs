using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para janelas de freeze.</summary>
public interface IFreezeWindowRepository
{
    /// <summary>Busca uma janela de freeze pelo ID.</summary>
    Task<FreezeWindow?> GetByIdAsync(FreezeWindowId id, CancellationToken cancellationToken = default);

    /// <summary>Lista janelas de freeze ativas num determinado momento.</summary>
    Task<IReadOnlyList<FreezeWindow>> ListActiveAtAsync(DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Lista todas as janelas de freeze com paginação.</summary>
    Task<IReadOnlyList<FreezeWindow>> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Lista janelas de freeze que intersectam uma janela temporal, com filtros opcionais.</summary>
    Task<IReadOnlyList<FreezeWindow>> ListInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment,
        bool? isActive,
        CancellationToken cancellationToken = default);

    /// <summary>Conta o total de janelas de freeze com filtros opcionais.</summary>
    Task<int> CountAsync(
        string? environment,
        bool? isActive,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma janela de freeze.</summary>
    void Add(FreezeWindow window);

    /// <summary>Remove uma janela de freeze.</summary>
    void Remove(FreezeWindow window);
}
