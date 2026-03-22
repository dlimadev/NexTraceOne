using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

/// <summary>
/// Implementação da surface de incidentes para o subdomínio Reliability.
/// Acessa IncidentDbContext diretamente — permitido dentro do mesmo módulo OI.
/// </summary>
internal sealed class ReliabilityIncidentSurface(IncidentDbContext db) : IReliabilityIncidentSurface
{
    /// <summary>Statuses ativos para efeito de scoring de confiabilidade.</summary>
    private static readonly IncidentStatus[] ActiveStatuses =
    [
        IncidentStatus.Open, IncidentStatus.Investigating,
        IncidentStatus.Mitigating, IncidentStatus.Monitoring
    ];

    public async Task<IReadOnlyList<ReliabilityIncidentSignal>> GetActiveIncidentsAsync(
        string serviceName, Guid tenantId, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var incidents = await db.Incidents
            .AsNoTracking()
            .Where(i => i.ServiceId == serviceName
                && i.TenantId == tenantId
                && ActiveStatuses.Contains(i.Status)
                && i.DetectedAt >= cutoff)
            .Select(i => new { i.ServiceId, i.ServiceName, i.OwnerTeam, i.Severity, i.Status, i.DetectedAt })
            .ToListAsync(ct);

        return incidents.Select(i => new ReliabilityIncidentSignal(
            i.ServiceId,
            i.ServiceName,
            i.OwnerTeam,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.DetectedAt)).ToList();
    }

    public async Task<IReadOnlyList<ReliabilityIncidentSignal>> GetAllServicesIncidentSignalsAsync(
        Guid tenantId, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var incidents = await db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && ActiveStatuses.Contains(i.Status)
                && i.DetectedAt >= cutoff)
            .Select(i => new { i.ServiceId, i.ServiceName, i.OwnerTeam, i.Severity, i.Status, i.DetectedAt })
            .ToListAsync(ct);

        return incidents.Select(i => new ReliabilityIncidentSignal(
            i.ServiceId,
            i.ServiceName,
            i.OwnerTeam,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.DetectedAt)).ToList();
    }

    public async Task<IReadOnlyList<ReliabilityIncidentSignal>> GetTeamIncidentsAsync(
        string teamId, Guid tenantId, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var incidents = await db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && i.OwnerTeam == teamId
                && ActiveStatuses.Contains(i.Status)
                && i.DetectedAt >= cutoff)
            .Select(i => new { i.ServiceId, i.ServiceName, i.OwnerTeam, i.Severity, i.Status, i.DetectedAt })
            .ToListAsync(ct);

        return incidents.Select(i => new ReliabilityIncidentSignal(
            i.ServiceId,
            i.ServiceName,
            i.OwnerTeam,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.DetectedAt)).ToList();
    }

    public async Task<IReadOnlyList<ReliabilityIncidentSignal>> GetDomainIncidentsAsync(
        string domainId, Guid tenantId, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var incidents = await db.Incidents
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && i.ImpactedDomain == domainId
                && ActiveStatuses.Contains(i.Status)
                && i.DetectedAt >= cutoff)
            .Select(i => new { i.ServiceId, i.ServiceName, i.OwnerTeam, i.Severity, i.Status, i.DetectedAt })
            .ToListAsync(ct);

        return incidents.Select(i => new ReliabilityIncidentSignal(
            i.ServiceId,
            i.ServiceName,
            i.OwnerTeam,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.DetectedAt)).ToList();
    }

    public async Task<bool> HasRunbookAsync(string serviceId, CancellationToken ct)
        => await db.Runbooks
            .AsNoTracking()
            .AnyAsync(r => r.LinkedService == serviceId, ct);
}
