using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para registos de verificação de contrato provenientes de CI/CD.
/// </summary>
public interface IContractVerificationRepository
{
    /// <summary>Obtém uma verificação de contrato pelo seu identificador.</summary>
    Task<ContractVerification?> GetByIdAsync(ContractVerificationId id, CancellationToken cancellationToken);

    /// <summary>Lista verificações de contrato por ativo de API.</summary>
    Task<IReadOnlyList<ContractVerification>> ListByApiAssetAsync(string apiAssetId, CancellationToken cancellationToken);

    /// <summary>Lista verificações de contrato por serviço com paginação.</summary>
    Task<IReadOnlyList<ContractVerification>> ListByServiceAsync(string serviceName, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Obtém a verificação mais recente para um ativo de API.</summary>
    Task<ContractVerification?> GetLatestByApiAssetAsync(string apiAssetId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo registo de verificação de contrato.</summary>
    Task AddAsync(ContractVerification verification, CancellationToken cancellationToken);
}
