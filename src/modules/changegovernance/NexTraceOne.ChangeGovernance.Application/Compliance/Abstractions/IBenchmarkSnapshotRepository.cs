using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>Repositório de snapshots de métricas DORA para benchmarks cross-tenant.</summary>
public interface IBenchmarkSnapshotRepository
{
    /// <summary>Obtém um snapshot pelo seu identificador.</summary>
    Task<BenchmarkSnapshotRecord?> GetByIdAsync(BenchmarkSnapshotRecordId id, CancellationToken ct = default);

    /// <summary>Lista snapshots de um tenant no período especificado.</summary>
    Task<IReadOnlyList<BenchmarkSnapshotRecord>> ListByTenantAsync(string tenantId, DateTimeOffset since, CancellationToken ct = default);

    /// <summary>
    /// Lista todos os snapshots marcados como anonimizados no período especificado.
    /// Usado para cálculo de benchmarks cross-tenant — nunca retorna dados não anonimizados.
    /// </summary>
    Task<IReadOnlyList<BenchmarkSnapshotRecord>> ListAnonymizedAsync(DateTimeOffset since, CancellationToken ct = default);

    /// <summary>Adiciona um novo snapshot.</summary>
    void Add(BenchmarkSnapshotRecord snapshot);
}
