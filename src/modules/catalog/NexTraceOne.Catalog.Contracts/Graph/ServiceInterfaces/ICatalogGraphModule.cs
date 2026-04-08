using NexTraceOne.Catalog.Contracts.Graph.DTOs;

namespace NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Catalog Graph.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface ICatalogGraphModule
{
    /// <summary>Verifica se um ativo de API existe pelo seu identificador.</summary>
    Task<bool> ApiAssetExistsAsync(Guid apiAssetId, CancellationToken cancellationToken);

    /// <summary>Verifica se um ativo de serviço existe pelo nome único.</summary>
    Task<bool> ServiceAssetExistsAsync(string serviceName, CancellationToken cancellationToken);

    /// <summary>Conta o total de serviços associados a uma equipa.</summary>
    Task<int> CountServicesByTeamAsync(string teamName, CancellationToken cancellationToken);

    /// <summary>Conta o total de serviços associados a um domínio.</summary>
    Task<int> CountServicesByDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Lista serviços associados a uma equipa.</summary>
    Task<IReadOnlyList<TeamServiceInfo>> ListServicesByTeamAsync(string teamName, CancellationToken cancellationToken);

    /// <summary>Lista contratos associados a uma equipa.</summary>
    Task<IReadOnlyList<TeamContractInfo>> ListContractsByTeamAsync(string teamName, CancellationToken cancellationToken);

    /// <summary>Lista todos os serviços registados no catálogo.</summary>
    Task<IReadOnlyList<TeamServiceInfo>> ListAllServicesAsync(CancellationToken cancellationToken);

    /// <summary>Lista serviços associados a um domínio.</summary>
    Task<IReadOnlyList<TeamServiceInfo>> ListServicesByDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Lista dependências cross-team de uma equipa.</summary>
    Task<IReadOnlyList<CrossTeamDependencyInfo>> ListCrossTeamDependenciesAsync(string teamName, CancellationToken cancellationToken);
}
