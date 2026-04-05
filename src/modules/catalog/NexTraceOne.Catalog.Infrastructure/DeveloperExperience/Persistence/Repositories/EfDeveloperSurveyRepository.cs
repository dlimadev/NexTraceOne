using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Repositories;

/// <summary>Repositório EF Core para DeveloperSurvey.</summary>
internal sealed class EfDeveloperSurveyRepository(DeveloperExperienceDbContext context) : IDeveloperSurveyRepository
{
    public async Task AddAsync(DeveloperSurvey survey, CancellationToken cancellationToken = default)
    {
        await context.DeveloperSurveys.AddAsync(survey, cancellationToken);
    }

    public async Task<IReadOnlyList<DeveloperSurvey>> ListByTeamAsync(
        string teamId,
        string? period,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.DeveloperSurveys
            .AsNoTracking()
            .Where(s => s.TeamId == teamId);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(s => s.Period == period);

        return await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeveloperSurvey>> ListAsync(
        string? teamId,
        string? period,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.DeveloperSurveys.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(teamId))
            query = query.Where(s => s.TeamId == teamId);
        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(s => s.Period == period);

        return await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
