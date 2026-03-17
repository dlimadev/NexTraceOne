using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade ServiceCostProfile.
/// Provê operações de leitura e escrita para perfis de custo de serviços.
/// </summary>
public interface IServiceCostProfileRepository
{
    /// <summary>Busca um perfil de custo pelo seu identificador.</summary>
    Task<ServiceCostProfile?> GetByIdAsync(ServiceCostProfileId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca o perfil de custo de um serviço e ambiente específicos.
    /// Retorna null se o perfil ainda não foi criado.
    /// </summary>
    Task<ServiceCostProfile?> GetByServiceAndEnvironmentAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo perfil de custo ao repositório.</summary>
    void Add(ServiceCostProfile profile);
}
