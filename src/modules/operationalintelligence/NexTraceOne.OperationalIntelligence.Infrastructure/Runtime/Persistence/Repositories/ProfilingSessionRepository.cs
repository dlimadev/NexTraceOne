using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

internal sealed class ProfilingSessionRepository(RuntimeIntelligenceDbContext context) : IProfilingSessionRepository
{
    public async Task<ProfilingSession?> GetByIdAsync(ProfilingSessionId id, CancellationToken ct = default)
        => await context.ProfilingSessions.FindAsync([id.Value], ct);

    public async Task<IReadOnlyList<ProfilingSession>> ListByServiceAsync(
        string serviceName, string environment, int page, int pageSize, CancellationToken ct = default)
        => await context.ProfilingSessions
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.WindowStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<ProfilingSession?> GetLatestByServiceAsync(
        string serviceName, string environment, CancellationToken ct = default)
        => await context.ProfilingSessions
            .Where(s => s.ServiceName == serviceName && s.Environment == environment)
            .OrderByDescending(s => s.WindowStart)
            .FirstOrDefaultAsync(ct);

    public void Add(ProfilingSession session) => context.ProfilingSessions.Add(session);
    public void Update(ProfilingSession session) => context.ProfilingSessions.Update(session);
}
