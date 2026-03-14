using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Enums;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.GetLicenseHealth;

/// <summary>
/// Feature: GetLicenseHealth — calcula e retorna o health score consolidado da licença.
///
/// O health score é um indicador 0.0 a 1.0 que considera:
/// - Tempo restante até expiração (40%)
/// - Consumo médio das quotas (40%)
/// - Estado geral: ativo e hardware vinculado (20%)
///
/// Usado em dashboards para visibilidade rápida da saúde da licença
/// e como input para warnings proativos e CTAs de expansão.
/// </summary>
public static class GetLicenseHealth
{
    /// <summary>Query de health score da licença.</summary>
    public sealed record Query(string LicenseKey) : IQuery<Response>;

    /// <summary>Valida a entrada da query de health.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que calcula o health score da licença.</summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(request.LicenseKey, cancellationToken);
            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            var now = dateTimeProvider.UtcNow;
            var healthScore = license.CalculateHealthScore(now);
            var daysRemaining = license.DaysUntilExpiration(now);
            var isInGracePeriod = license.IsInGracePeriod(now);

            var quotaWarnings = license.UsageQuotas
                .Where(q => q.GetWarningLevel() >= WarningLevel.Advisory)
                .Select(q => new QuotaWarning(
                    q.MetricCode,
                    q.CurrentUsage,
                    q.Limit,
                    q.UsagePercentage,
                    q.GetWarningLevel().ToString(),
                    q.EnforcementLevel.ToString()))
                .ToArray();

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                healthScore,
                license.IsActive,
                license.IsExpired(now),
                isInGracePeriod,
                daysRemaining,
                license.Type.ToString(),
                license.Edition.ToString(),
                license.IsTrial,
                license.TrialConverted,
                quotaWarnings);
        }
    }

    /// <summary>Resposta do health score da licença com warnings detalhados.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        decimal HealthScore,
        bool IsActive,
        bool IsExpired,
        bool IsInGracePeriod,
        int DaysRemaining,
        string LicenseType,
        string Edition,
        bool IsTrial,
        bool TrialConverted,
        IReadOnlyList<QuotaWarning> QuotaWarnings);

    /// <summary>Warning individual de quota com nível de alerta e enforcement.</summary>
    public sealed record QuotaWarning(
        string MetricCode,
        long CurrentUsage,
        long Limit,
        decimal UsagePercentage,
        string WarningLevel,
        string EnforcementLevel);
}
