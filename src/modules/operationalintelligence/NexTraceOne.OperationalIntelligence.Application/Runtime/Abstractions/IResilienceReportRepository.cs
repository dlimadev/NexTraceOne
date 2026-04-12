using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Interface do repositório de ResilienceReport.
/// Define operações CRUD e consultas para relatórios de resiliência pós-experimento.
/// </summary>
public interface IResilienceReportRepository
{
    /// <summary>Obtém um relatório pelo identificador.</summary>
    Task<ResilienceReport?> GetByIdAsync(ResilienceReportId id, CancellationToken ct);

    /// <summary>Obtém relatórios associados a um experimento de chaos específico.</summary>
    Task<IReadOnlyList<ResilienceReport>> GetByExperimentIdAsync(Guid experimentId, CancellationToken ct);

    /// <summary>Lista relatórios, opcionalmente filtrados por nome de serviço.</summary>
    Task<IReadOnlyList<ResilienceReport>> ListByServiceAsync(string? serviceName, CancellationToken ct);

    /// <summary>Adiciona um novo relatório.</summary>
    Task AddAsync(ResilienceReport report, CancellationToken ct);
}
