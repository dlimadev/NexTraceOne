using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de expectativas de consumidores de contratos.
/// Suporta Consumer-Driven Contract Testing (CDCT).
/// </summary>
public interface IConsumerExpectationRepository
{
    /// <summary>Adiciona uma nova expectativa de consumidor.</summary>
    void Add(ConsumerExpectation expectation);

    /// <summary>Busca uma expectativa pelo seu identificador.</summary>
    Task<ConsumerExpectation?> GetByIdAsync(ConsumerExpectationId id, CancellationToken ct = default);

    /// <summary>Lista expectativas activas para um contrato (ApiAssetId).</summary>
    Task<IReadOnlyList<ConsumerExpectation>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);

    /// <summary>Lista expectativas activas para um contrato por serviço consumidor.</summary>
    Task<ConsumerExpectation?> GetByApiAssetAndConsumerAsync(
        Guid apiAssetId,
        string consumerServiceName,
        CancellationToken ct = default);
}
