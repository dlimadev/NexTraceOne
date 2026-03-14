using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Application.Abstractions;

/// <summary>
/// Contrato de repositório para a entidade RuntimeBaseline.
/// Provê operações de leitura e escrita para baselines de métricas de runtime de serviços.
/// </summary>
public interface IRuntimeBaselineRepository
{
    /// <summary>Busca uma baseline de runtime pelo seu identificador.</summary>
    Task<RuntimeBaseline?> GetByIdAsync(RuntimeBaselineId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca a baseline de runtime de um serviço e ambiente específicos.
    /// Retorna null se a baseline ainda não foi estabelecida.
    /// </summary>
    Task<RuntimeBaseline?> GetByServiceAndEnvironmentAsync(
        string serviceName,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova baseline de runtime ao repositório.</summary>
    void Add(RuntimeBaseline baseline);
}
