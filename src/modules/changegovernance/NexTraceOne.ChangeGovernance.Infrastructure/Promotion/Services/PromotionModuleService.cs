using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.ChangeGovernance.Contracts.Promotion.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Services;

/// <summary>
/// Implementação do contrato público do módulo Promotion.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios diretamente.
/// </summary>
internal sealed class PromotionModuleService(
    PromotionDbContext context,
    ILogger<PromotionModuleService> logger) : IPromotionModule
{
    /// <inheritdoc />
    public async Task<bool> IsPromotionApprovedAsync(
        Guid releaseId,
        Guid targetEnvironmentId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Checking promotion approval for release {ReleaseId} to environment {EnvironmentId}",
            releaseId, targetEnvironmentId);

        return await context.PromotionRequests
            .AsNoTracking()
            .AnyAsync(
                r => r.ReleaseId == releaseId
                  && r.TargetEnvironmentId.Value == targetEnvironmentId
                  && r.Status == Domain.Promotion.Enums.PromotionStatus.Approved,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetPromotionStatusAsync(
        Guid promotionRequestId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching promotion status for request {PromotionRequestId}", promotionRequestId);

        return await context.PromotionRequests
            .AsNoTracking()
            .Where(r => r.Id.Value == promotionRequestId)
            .Select(r => r.Status.ToString())
            .FirstOrDefaultAsync(cancellationToken);
    }
}
