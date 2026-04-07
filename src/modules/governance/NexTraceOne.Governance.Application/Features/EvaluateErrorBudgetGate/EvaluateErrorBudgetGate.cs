using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.EvaluateErrorBudgetGate;

/// <summary>
/// Feature: EvaluateErrorBudgetGate — avalia se o error budget do serviço permite deploys.
/// Consulta parâmetros:
///   - reliability.error_budget.auto_block_deploys
///   - reliability.error_budget.block_threshold_pct
/// Quando o error budget restante está abaixo do threshold, bloqueia deploys automaticamente.
/// </summary>
public static class EvaluateErrorBudgetGate
{
    /// <summary>Query para avaliar o gate de error budget.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        decimal ErrorBudgetRemainingPct) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ErrorBudgetRemainingPct).InclusiveBetween(0, 100);
        }
    }

    /// <summary>Handler que avalia o error budget gate.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Check if auto-block is enabled
            var autoBlockConfig = await configService.ResolveEffectiveValueAsync(
                "reliability.error_budget.auto_block_deploys",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var isAutoBlockEnabled = autoBlockConfig?.EffectiveValue == "true";

            if (!isAutoBlockEnabled)
            {
                return new Response(
                    ServiceName: request.ServiceName,
                    Environment: request.Environment,
                    ErrorBudgetRemainingPct: request.ErrorBudgetRemainingPct,
                    BlockThresholdPct: 0,
                    IsBlocked: false,
                    Reason: "Error budget auto-blocking is not enabled",
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Get threshold
            var thresholdConfig = await configService.ResolveEffectiveValueAsync(
                "reliability.error_budget.block_threshold_pct",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var threshold = decimal.TryParse(thresholdConfig?.EffectiveValue, out var t) ? t : 10m;

            var isBlocked = request.ErrorBudgetRemainingPct < threshold;

            var reason = isBlocked
                ? $"Deploy blocked: error budget remaining ({request.ErrorBudgetRemainingPct:F1}%) is below threshold ({threshold:F1}%)"
                : $"Deploy allowed: error budget remaining ({request.ErrorBudgetRemainingPct:F1}%) is above threshold ({threshold:F1}%)";

            return new Response(
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                ErrorBudgetRemainingPct: request.ErrorBudgetRemainingPct,
                BlockThresholdPct: threshold,
                IsBlocked: isBlocked,
                Reason: reason,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação do gate de error budget.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        decimal ErrorBudgetRemainingPct,
        decimal BlockThresholdPct,
        bool IsBlocked,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
