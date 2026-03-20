using NexTraceOne.AIKnowledge.Application.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Context;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação de IPromotionRiskContextBuilder.
/// Constrói contextos de análise de risco de promoção e comparação cross-environment,
/// sempre validando isolamento por tenant.
/// </summary>
internal sealed class PromotionRiskContextBuilder(
    IAIContextBuilder aiContextBuilder,
    ITenantEnvironmentContextResolver contextResolver) : IPromotionRiskContextBuilder
{
    /// <inheritdoc />
    public async Task<PromotionRiskAnalysisContext> BuildAsync(
        TenantId tenantId,
        EnvironmentId sourceEnvironmentId,
        EnvironmentId targetEnvironmentId,
        string serviceName,
        string version,
        Guid? releaseId = null,
        int observationWindowDays = 7,
        CancellationToken cancellationToken = default)
    {
        var sourceContext = await contextResolver.ResolveAsync(
            tenantId, sourceEnvironmentId, cancellationToken);

        var targetContext = await contextResolver.ResolveAsync(
            tenantId, targetEnvironmentId, cancellationToken);

        if (sourceContext is null)
            throw new InvalidOperationException(
                $"Source environment {sourceEnvironmentId.Value:N} does not exist or does not belong to tenant {tenantId.Value:N}.");

        if (targetContext is null)
            throw new InvalidOperationException(
                $"Target environment {targetEnvironmentId.Value:N} does not exist or does not belong to tenant {tenantId.Value:N}.");

        var executionContext = await aiContextBuilder.BuildForAsync(
            tenantId, sourceEnvironmentId, "promotion-risk", cancellationToken);

        return PromotionRiskAnalysisContext.Create(
            executionContext,
            sourceEnvironmentId,
            sourceContext.Profile,
            targetEnvironmentId,
            targetContext.Profile,
            serviceName,
            version,
            AiTimeWindow.LastDays(observationWindowDays),
            releaseId);
    }

    /// <inheritdoc />
    public async Task<EnvironmentComparisonContext> BuildComparisonAsync(
        TenantId tenantId,
        EnvironmentId subjectEnvironmentId,
        EnvironmentId referenceEnvironmentId,
        IEnumerable<string>? serviceFilter = null,
        IEnumerable<ComparisonDimension>? dimensions = null,
        CancellationToken cancellationToken = default)
    {
        var subjectContext = await contextResolver.ResolveAsync(
            tenantId, subjectEnvironmentId, cancellationToken);

        var referenceContext = await contextResolver.ResolveAsync(
            tenantId, referenceEnvironmentId, cancellationToken);

        if (subjectContext is null)
            throw new InvalidOperationException(
                $"Subject environment {subjectEnvironmentId.Value:N} does not exist or does not belong to tenant {tenantId.Value:N}.");

        if (referenceContext is null)
            throw new InvalidOperationException(
                $"Reference environment {referenceEnvironmentId.Value:N} does not exist or does not belong to tenant {tenantId.Value:N}.");

        var executionContext = await aiContextBuilder.BuildForAsync(
            tenantId, subjectEnvironmentId, "environment-comparison", cancellationToken);

        return EnvironmentComparisonContext.Create(
            executionContext,
            subjectEnvironmentId,
            subjectContext.Profile,
            referenceEnvironmentId,
            referenceContext.Profile,
            serviceFilter,
            dimensions);
    }
}
