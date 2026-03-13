using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.AlertLicenseThreshold;

/// <summary>
/// Feature: AlertLicenseThreshold — retorna quotas que atingiram o threshold de alerta.
/// </summary>
public static class AlertLicenseThreshold
{
    /// <summary>Query de quotas em threshold de alerta.</summary>
    public sealed record Query(string LicenseKey) : IQuery<Response>;

    /// <summary>Valida a entrada da query de threshold.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que retorna as quotas em threshold de alerta da licença.</summary>
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

            if (!license.IsActive)
            {
                return LicensingErrors.LicenseInactive();
            }

            if (license.ExpiresAt <= dateTimeProvider.UtcNow)
            {
                return LicensingErrors.LicenseExpired(license.ExpiresAt);
            }

            var quotas = license.UsageQuotas
                .Where(quota => quota.IsThresholdReached())
                .Select(quota => new ThresholdItem(quota.MetricCode, quota.CurrentUsage, quota.Limit))
                .ToArray();

            return new Response(license.LicenseKey, quotas);
        }
    }

    /// <summary>Resposta com as quotas que atingiram threshold.</summary>
    public sealed record Response(string LicenseKey, IReadOnlyList<ThresholdItem> Thresholds);

    /// <summary>Item de quota em threshold de alerta.</summary>
    public sealed record ThresholdItem(string MetricCode, long CurrentUsage, long Limit);
}
