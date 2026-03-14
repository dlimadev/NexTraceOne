using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Application.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade ObservabilityProfile.
/// Provê operações de leitura e escrita para perfis de maturidade de observabilidade de serviços.
/// </summary>
public interface IObservabilityProfileRepository
{
    /// <summary>Busca um perfil de observabilidade pelo seu identificador.</summary>
    Task<ObservabilityProfile?> GetByIdAsync(ObservabilityProfileId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca o perfil de observabilidade de um serviço e ambiente específicos.
    /// Retorna null se o perfil ainda não foi avaliado.
    /// </summary>
    Task<ObservabilityProfile?> GetByServiceAndEnvironmentAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo perfil de observabilidade ao repositório.</summary>
    void Add(ObservabilityProfile profile);
}
