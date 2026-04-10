using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Interface do repositório de EnvironmentDriftReport.
/// Define operações CRUD e consultas para relatórios de drift entre ambientes.
/// </summary>
public interface IEnvironmentDriftReportRepository
{
    /// <summary>Obtém um relatório pelo identificador.</summary>
    Task<EnvironmentDriftReport?> GetByIdAsync(EnvironmentDriftReportId id, CancellationToken ct);

    /// <summary>Lista relatórios, opcionalmente filtrados por ambientes e/ou status.</summary>
    Task<IReadOnlyList<EnvironmentDriftReport>> ListAsync(
        string? sourceEnvironment,
        string? targetEnvironment,
        DriftReportStatus? status,
        CancellationToken ct);

    /// <summary>Obtém o relatório mais recente para um par de ambientes.</summary>
    Task<EnvironmentDriftReport?> GetLatestAsync(
        string sourceEnvironment,
        string targetEnvironment,
        CancellationToken ct);

    /// <summary>Adiciona um novo relatório.</summary>
    void Add(EnvironmentDriftReport report);

    /// <summary>Atualiza um relatório existente.</summary>
    void Update(EnvironmentDriftReport report);
}
