using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using NexTraceOne.Identity.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Handler especializado na expiração de acessos temporários a ambientes (EnvironmentAccess).
/// Desativa concessões cujo prazo (ExpiresAt) já passou, usando o método de domínio Revoke().
/// Registra SecurityEvent com risco baixo (10) — o acesso simplesmente expirou conforme configurado.
/// </summary>
public sealed class EnvironmentAccessExpirationHandler : IExpirationHandler
{
    /// <inheritdoc />
    public async Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = await dbContext.EnvironmentAccesses
            .Where(ea => ea.IsActive
                         && ea.ExpiresAt != null
                         && ea.ExpiresAt <= now)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        foreach (var access in expired)
        {
            access.Revoke(now);

            dbContext.SecurityEvents.Add(SecurityEvent.Create(
                access.TenantId,
                access.UserId,
                sessionId: null,
                SecurityEventType.EnvironmentAccessExpired,
                $"Environment access {access.Id.Value} for user {access.UserId.Value} in environment {access.EnvironmentId.Value} expired automatically.",
                riskScore: 10,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    environmentAccessId = access.Id.Value,
                    userId = access.UserId.Value,
                    environmentId = access.EnvironmentId.Value,
                    accessLevel = access.AccessLevel
                }),
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return expired.Count;
    }
}
