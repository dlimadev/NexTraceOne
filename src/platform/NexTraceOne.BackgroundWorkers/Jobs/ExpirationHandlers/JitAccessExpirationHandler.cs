using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Handler especializado na expiração de solicitações JIT Access.
/// Processa dois cenários distintos:
/// 1. Pendentes cujo prazo de aprovação (ApprovalDeadline) expirou.
/// 2. Aprovadas cujo prazo de acesso (GrantedUntil) expirou.
/// Registra SecurityEvent com risco moderado (20) e detalha o motivo da expiração.
/// </summary>
public sealed class JitAccessExpirationHandler : IExpirationHandler
{
    /// <inheritdoc />
    public async Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = await dbContext.JitAccessRequests
            .Where(r =>
                (r.Status == JitAccessStatus.Pending && r.ApprovalDeadline <= now) ||
                (r.Status == JitAccessStatus.Approved && r.GrantedUntil != null && r.GrantedUntil <= now))
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        foreach (var request in expired)
        {
            var previousStatus = request.Status;
            request.Expire(now);

            var reason = previousStatus == JitAccessStatus.Pending
                ? "approval deadline exceeded"
                : "access grant period ended";

            dbContext.SecurityEvents.Add(SecurityEvent.Create(
                request.TenantId,
                request.RequestedBy,
                sessionId: null,
                SecurityEventType.JitAccessExpired,
                $"JIT access request {request.Id.Value} for user {request.RequestedBy.Value} expired ({reason}).",
                riskScore: 20,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    jitAccessRequestId = request.Id.Value,
                    requestedBy = request.RequestedBy.Value,
                    previousStatus = previousStatus.ToString(),
                    permissionCode = request.PermissionCode
                }),
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return expired.Count;
    }
}
