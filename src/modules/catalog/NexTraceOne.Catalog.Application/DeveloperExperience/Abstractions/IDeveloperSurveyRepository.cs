using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>Contrato de repositório para DeveloperSurvey.</summary>
public interface IDeveloperSurveyRepository
{
    Task AddAsync(DeveloperSurvey survey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeveloperSurvey>> ListByTeamAsync(string teamId, string? period, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeveloperSurvey>> ListAsync(string? teamId, string? period, int page, int pageSize, CancellationToken cancellationToken = default);
}
