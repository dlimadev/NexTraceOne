using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que processa expirações de entidades do módulo Identity.
///
/// Responsabilidades:
/// 1. Expirar delegações (Delegation) cuja vigência terminou.
/// 2. Expirar solicitações Break Glass cujo prazo encerrou.
/// 3. Expirar solicitações JIT Access pendentes além do deadline de aprovação e grants expirados.
/// 4. Processar campanhas de Access Review cujo prazo expirou (auto-revogação de itens pendentes).
/// 5. Desativar EnvironmentAccess cujo prazo de concessão temporária expirou.
///
/// Cada processamento gera SecurityEvent para trilha de auditoria completa.
///
/// Design:
/// - Executa a cada 60 segundos (configurável).
/// - Usa lote para minimizar pressão no banco.
/// - Idempotente: entidades já expiradas são ignoradas por seus métodos Expire().
/// - Falhas em um lote não afetam outros lotes.
/// </summary>
public sealed class IdentityExpirationJob(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    ILogger<IdentityExpirationJob> logger) : BackgroundService
{
    private const int BatchSize = 100;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpirationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in Identity expiration job.");
            }
        }
    }

    /// <summary>
    /// Orquestra todas as expirações num único ciclo.
    /// Cada tipo de entidade é processado independentemente — falha em um não impede os demais.
    /// </summary>
    private async Task ProcessExpirationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = dateTimeProvider.UtcNow;

        await ExpireDelegationsAsync(dbContext, now, cancellationToken);
        await ExpireBreakGlassRequestsAsync(dbContext, now, cancellationToken);
        await ExpireJitAccessRequestsAsync(dbContext, now, cancellationToken);
        await ProcessAccessReviewDeadlinesAsync(dbContext, now, cancellationToken);
        await ExpireEnvironmentAccessesAsync(dbContext, now, cancellationToken);
    }

    /// <summary>
    /// Expira delegações cuja vigência (ValidUntil) já foi ultrapassada.
    /// Gera SecurityEvent do tipo DelegationExpired para cada delegação expirada.
    /// </summary>
    private async Task ExpireDelegationsAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var expired = await dbContext.Delegations
                .Where(d => d.Status == DelegationStatus.Active && d.ValidUntil <= now)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
                return;

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

            logger.LogInformation("Expired {Count} delegation(s)", expired.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to expire delegations.");
        }
    }

    /// <summary>
    /// Expira solicitações Break Glass cuja janela de acesso emergencial já encerrou.
    /// Gera SecurityEvent do tipo BreakGlassExpired para cada solicitação expirada.
    /// </summary>
    private async Task ExpireBreakGlassRequestsAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var expired = await dbContext.BreakGlassRequests
                .Where(r => r.Status == BreakGlassStatus.Active
                            && r.ExpiresAt != null
                            && r.ExpiresAt <= now)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
                return;

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

            logger.LogInformation("Expired {Count} Break Glass request(s)", expired.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to expire Break Glass requests.");
        }
    }

    /// <summary>
    /// Expira solicitações JIT Access em dois cenários:
    /// - Pendentes cujo prazo de aprovação (ApprovalDeadline) expirou.
    /// - Aprovadas cujo prazo de acesso (GrantedUntil) expirou.
    /// Gera SecurityEvent do tipo JitAccessExpired para cada solicitação expirada.
    /// </summary>
    private async Task ExpireJitAccessRequestsAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var expired = await dbContext.JitAccessRequests
                .Where(r =>
                    (r.Status == JitAccessStatus.Pending && r.ApprovalDeadline <= now) ||
                    (r.Status == JitAccessStatus.Approved && r.GrantedUntil != null && r.GrantedUntil <= now))
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
                return;

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

            logger.LogInformation("Expired {Count} JIT access request(s)", expired.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to expire JIT access requests.");
        }
    }

    /// <summary>
    /// Processa campanhas de Access Review cujo prazo (Deadline) já foi ultrapassado.
    /// Itens não revisados são auto-revogados pelo método ProcessDeadline da campanha.
    /// Gera SecurityEvent do tipo AccessReviewExpiredAutoRevoked para cada campanha processada.
    /// </summary>
    private async Task ProcessAccessReviewDeadlinesAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdueCampaigns = await dbContext.AccessReviewCampaigns
                .Include(c => c.Items)
                .Where(c => c.Status == AccessReviewCampaignStatus.Open && c.Deadline <= now)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (overdueCampaigns.Count == 0)
                return;

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

            logger.LogInformation(
                "Processed {Count} overdue access review campaign(s)",
                overdueCampaigns.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process access review deadlines.");
        }
    }

    /// <summary>
    /// Desativa EnvironmentAccess cujo prazo de concessão temporária (ExpiresAt) já passou.
    /// Gera SecurityEvent do tipo EnvironmentAccessExpired para cada acesso expirado.
    /// </summary>
    private async Task ExpireEnvironmentAccessesAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            var expired = await dbContext.EnvironmentAccesses
                .Where(ea => ea.IsActive
                             && ea.ExpiresAt != null
                             && ea.ExpiresAt <= now)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
                return;

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

            logger.LogInformation("Expired {Count} environment access(es)", expired.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to expire environment accesses.");
        }
    }
}
