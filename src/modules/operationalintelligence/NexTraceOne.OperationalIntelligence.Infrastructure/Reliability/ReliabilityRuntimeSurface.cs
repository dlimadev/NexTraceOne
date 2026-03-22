using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

/// <summary>
/// Implementação da surface de runtime para o subdomínio Reliability.
/// Acessa RuntimeIntelligenceDbContext diretamente — permitido dentro do mesmo módulo OI.
/// RuntimeSnapshot usa RLS via variável de sessão — sem filtro explícito de TenantId.
/// </summary>
internal sealed class ReliabilityRuntimeSurface(
    RuntimeIntelligenceDbContext db,
    ICurrentTenant tenant) : IReliabilityRuntimeSurface
{
    public async Task<RuntimeServiceSignal?> GetLatestSignalAsync(
        string serviceName, string environment, CancellationToken ct)
    {
        var snapshot = await db.RuntimeSnapshots
            .AsNoTracking()
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(ct);

        return snapshot is null ? null : new RuntimeServiceSignal(
            snapshot.ServiceName,
            snapshot.Environment,
            snapshot.HealthStatus.ToString(),
            snapshot.ErrorRate,
            snapshot.P99LatencyMs,
            snapshot.RequestsPerSecond,
            snapshot.CapturedAt);
    }

    public async Task<IReadOnlyList<RuntimeServiceSignal>> GetLatestSignalsAllServicesAsync(
        string? environment, CancellationToken ct)
    {
        var query = db.RuntimeSnapshots.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(s => s.Environment == environment);

        // Obtém o snapshot mais recente por serviço.
        var latestSnapshots = await query
            .GroupBy(s => s.ServiceName)
            .Select(g => g.OrderByDescending(s => s.CapturedAt).First())
            .ToListAsync(ct);

        return latestSnapshots.Select(s => new RuntimeServiceSignal(
            s.ServiceName,
            s.Environment,
            s.HealthStatus.ToString(),
            s.ErrorRate,
            s.P99LatencyMs,
            s.RequestsPerSecond,
            s.CapturedAt)).ToList();
    }

    public async Task<decimal?> GetObservabilityScoreAsync(
        string serviceName, string environment, CancellationToken ct)
    {
        var profile = await db.ObservabilityProfiles
            .AsNoTracking()
            .Where(p => p.ServiceName == serviceName && p.Environment == environment)
            .OrderByDescending(p => p.LastAssessedAt)
            .FirstOrDefaultAsync(ct);

        return profile?.ObservabilityScore;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetObservabilityScoresAllServicesAsync(
        string? environment, CancellationToken ct)
    {
        var query = db.ObservabilityProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(p => p.Environment == environment);

        // Obtém o perfil mais recente por serviço e retorna score.
        var profiles = await query
            .GroupBy(p => p.ServiceName)
            .Select(g => g.OrderByDescending(p => p.LastAssessedAt).First())
            .Select(p => new { p.ServiceName, p.ObservabilityScore })
            .ToListAsync(ct);

        return profiles.ToDictionary(p => p.ServiceName, p => p.ObservabilityScore);
    }
}
