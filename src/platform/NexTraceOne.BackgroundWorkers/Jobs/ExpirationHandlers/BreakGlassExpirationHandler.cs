using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Handler especializado na expiração de solicitações Break Glass.
/// Processa requisições de acesso emergencial cujo prazo (ExpiresAt) já encerrou,
/// aplicando o método de domínio Expire() e registrando SecurityEvent
/// com risco elevado (30) devido à natureza crítica do acesso emergencial.
/// </summary>
public sealed class BreakGlassExpirationHandler : IExpirationHandler
{
    /// <inheritdoc />
    public async Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = await dbContext.BreakGlassRequests
            .Where(r => r.Status == BreakGlassStatus.Active
                        && r.ExpiresAt != null
                        && r.ExpiresAt <= now)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        foreach (var request in expired)
        {
            request.Expire(now);

            dbContext.SecurityEvents.Add(SecurityEvent.Create(
                request.TenantId,
                request.RequestedBy,
                sessionId: null,
                SecurityEventType.BreakGlassExpired,
                $"Break Glass request {request.Id.Value} for user {request.RequestedBy.Value} expired automatically.",
                riskScore: 30,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    breakGlassRequestId = request.Id.Value,
                    requestedBy = request.RequestedBy.Value,
                    activatedAt = request.ActivatedAt,
                    expiresAt = request.ExpiresAt
                }),
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return expired.Count;
    }
}
