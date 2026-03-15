using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Application.Abstractions;

/// <summary>
/// Repositório de modelos de IA do Model Registry.
/// Suporta listagem filtrada, consulta individual e persistência.
/// </summary>
public interface IAiModelRepository
{
    /// <summary>Lista modelos com filtros opcionais de provedor, tipo, estado e origem.</summary>
    Task<IReadOnlyList<AIModel>> ListAsync(
        string? provider,
        ModelType? modelType,
        ModelStatus? status,
        bool? isInternal,
        CancellationToken ct);

    /// <summary>Obtém um modelo pelo identificador fortemente tipado.</summary>
    Task<AIModel?> GetByIdAsync(AIModelId id, CancellationToken ct);

    /// <summary>Adiciona um novo modelo para persistência.</summary>
    Task AddAsync(AIModel model, CancellationToken ct);

    /// <summary>Atualiza um modelo existente.</summary>
    Task UpdateAsync(AIModel model, CancellationToken ct);
}
