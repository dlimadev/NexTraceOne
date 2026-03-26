using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de detalhes SOAP/WSDL de versões de contrato publicadas.
/// </summary>
public interface ISoapContractDetailRepository
{
    /// <summary>Adiciona um novo SoapContractDetail ao repositório.</summary>
    void Add(SoapContractDetail detail);

    /// <summary>Busca o SoapContractDetail pelo seu identificador único.</summary>
    Task<SoapContractDetail?> GetByIdAsync(SoapContractDetailId id, CancellationToken ct = default);

    /// <summary>Busca o SoapContractDetail associado a uma versão de contrato.</summary>
    Task<SoapContractDetail?> GetByContractVersionIdAsync(ContractVersionId contractVersionId, CancellationToken ct = default);
}
