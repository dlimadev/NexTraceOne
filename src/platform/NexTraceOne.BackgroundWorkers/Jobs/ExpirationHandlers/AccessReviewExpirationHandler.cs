using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using NexTraceOne.Identity.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Handler especializado no processamento de campanhas de Access Review expiradas.
/// Quando o prazo (Deadline) de uma campanha aberta é ultrapassado, itens pendentes
/// são auto-revogados pelo método de domínio ProcessDeadline().
/// Registra SecurityEvent com risco alto (40) devido ao impacto de revogação automática.
/// </summary>
public sealed class AccessReviewExpirationHandler : IExpirationHandler
{
    /// <inheritdoc />
    public async Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var overdueCampaigns = await dbContext.AccessReviewCampaigns
            .Include(c => c.Items)
            .Where(c => c.Status == AccessReviewCampaignStatus.Open && c.Deadline <= now)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (overdueCampaigns.Count == 0)
            return 0;

        foreach (var campaign in overdueCampaigns)
        {
            var pendingBefore = campaign.Items
                .Count(i => i.Decision == AccessReviewDecision.Pending);

            campaign.ProcessDeadline(now);

            var autoRevokedCount = campaign.Items
                .Count(i => i.Decision == AccessReviewDecision.AutoRevoked);

            if (autoRevokedCount > 0)
            {
                dbContext.SecurityEvents.Add(SecurityEvent.Create(
                    campaign.TenantId,
                    campaign.InitiatedBy,
                    sessionId: null,
                    SecurityEventType.AccessReviewExpiredAutoRevoked,
                    $"Access review campaign '{campaign.Name}' ({campaign.Id.Value}) deadline reached. {autoRevokedCount} item(s) auto-revoked.",
                    riskScore: 40,
                    ipAddress: null,
                    userAgent: null,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        campaignId = campaign.Id.Value,
                        campaignName = campaign.Name,
                        pendingBeforeCount = pendingBefore,
                        autoRevokedCount
                    }),
                    now));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return overdueCampaigns.Count;
    }
}
