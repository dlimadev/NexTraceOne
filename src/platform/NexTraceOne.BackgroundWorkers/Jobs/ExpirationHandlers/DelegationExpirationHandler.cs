using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Handler especializado na expiração de delegações (Delegation).
/// Processa delegações cuja vigência (ValidUntil) foi ultrapassada,
/// aplicando o método de domínio Expire() e registrando SecurityEvent
/// para trilha de auditoria completa.
/// </summary>
public sealed class DelegationExpirationHandler : IExpirationHandler
{
    /// <inheritdoc />
    public async Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = await dbContext.Delegations
            .Where(d => d.Status == DelegationStatus.Active && d.ValidUntil <= now)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        foreach (var delegation in expired)
        {
            delegation.Expire(now);

            dbContext.SecurityEvents.Add(SecurityEvent.Create(
                delegation.TenantId,
                delegation.DelegateeId,
                sessionId: null,
                SecurityEventType.DelegationExpired,
                $"Delegation {delegation.Id.Value} from user {delegation.GrantorId.Value} to user {delegation.DelegateeId.Value} expired automatically.",
                riskScore: 10,
                ipAddress: null,
                userAgent: null,
                metadataJson: JsonSerializer.Serialize(new
                {
                    delegationId = delegation.Id.Value,
                    grantorId = delegation.GrantorId.Value,
                    delegateeId = delegation.DelegateeId.Value
                }),
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return expired.Count;
    }
}
