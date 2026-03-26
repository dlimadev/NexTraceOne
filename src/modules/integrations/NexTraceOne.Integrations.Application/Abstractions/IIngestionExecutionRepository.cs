using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Interface do repositório de IngestionExecutions para o módulo Integrations.
/// Define operações CRUD e consultas para execuções de ingestão.
/// </summary>
public interface IIngestionExecutionRepository
{
    /// <summary>Lista execuções com filtros e paginação.</summary>
    Task<IReadOnlyList<IngestionExecution>> ListAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Conta total de execuções com filtros.</summary>
    Task<int> CountAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct);

    /// <summary>Lista execuções por conector.</summary>
    Task<IReadOnlyList<IngestionExecution>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        int limit,
        CancellationToken ct);

    /// <summary>Obtém uma execução pelo seu identificador.</summary>
    Task<IngestionExecution?> GetByIdAsync(IngestionExecutionId id, CancellationToken ct);

    /// <summary>Obtém a última execução de um conector.</summary>
    Task<IngestionExecution?> GetLastByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct);

    /// <summary>Adiciona uma nova execução ao repositório.</summary>
    Task AddAsync(IngestionExecution execution, CancellationToken ct);

    /// <summary>Atualiza uma execução existente.</summary>
    Task UpdateAsync(IngestionExecution execution, CancellationToken ct);

    /// <summary>Conta execuções por resultado num período.</summary>
    Task<int> CountByResultInPeriodAsync(
        ExecutionResult result,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}
