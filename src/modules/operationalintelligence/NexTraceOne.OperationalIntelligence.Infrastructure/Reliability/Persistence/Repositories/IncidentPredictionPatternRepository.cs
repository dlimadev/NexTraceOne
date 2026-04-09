using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para padrões preditivos de incidentes (IncidentPredictionPattern).
/// </summary>
internal sealed class IncidentPredictionPatternRepository(ReliabilityDbContext context)
    : IIncidentPredictionPatternRepository
{
    public async Task<IncidentPredictionPattern?> GetByIdAsync(IncidentPredictionPatternId id, CancellationToken ct)
        => await context.IncidentPredictionPatterns
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<IncidentPredictionPattern>> ListAsync(
        string? environment,
        PredictionPatternStatus? status,
        PredictionPatternType? patternType,
        CancellationToken ct)
    {
        var query = context.IncidentPredictionPatterns.AsQueryable();

        if (environment is not null)
            query = query.Where(p => p.Environment == environment);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (patternType.HasValue)
            query = query.Where(p => p.PatternType == patternType.Value);

        return await query
            .OrderByDescending(p => p.DetectedAt)
            .ToListAsync(ct);
    }

    public async Task<IncidentPredictionPattern?> GetLatestByServiceAsync(
        string serviceId, string environment, CancellationToken ct)
        => await context.IncidentPredictionPatterns
            .Where(p => p.ServiceId == serviceId && p.Environment == environment)
            .OrderByDescending(p => p.DetectedAt)
            .FirstOrDefaultAsync(ct);

    public void Add(IncidentPredictionPattern pattern)
        => context.IncidentPredictionPatterns.Add(pattern);

    public void Update(IncidentPredictionPattern pattern)
        => context.IncidentPredictionPatterns.Update(pattern);
}
