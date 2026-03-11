using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.TrackUsageMetric;

/// <summary>
/// Feature: TrackUsageMetric — registra consumo de uma métrica licenciada.
/// </summary>
public static class TrackUsageMetric
{
    /// <summary>Comando de registro de uso.</summary>
    public sealed record Command(string LicenseKey, string MetricCode, long Quantity) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de uso.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MetricCode).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }

    /// <summary>Handler que registra o consumo da licença para a métrica informada.</summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IHardwareFingerprintProvider hardwareFingerprintProvider,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(request.LicenseKey, cancellationToken);
            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            var hardwareFingerprint = hardwareFingerprintProvider.Generate();
            var verificationResult = license.VerifyAt(dateTimeProvider.UtcNow, hardwareFingerprint);
            if (verificationResult.IsFailure)
            {
                return verificationResult.Error;
            }

            var usageResult = license.TrackUsage(request.MetricCode, request.Quantity, dateTimeProvider.UtcNow);
            if (usageResult.IsFailure)
            {
                return usageResult.Error;
            }

            return new Response(
                usageResult.Value.MetricCode,
                usageResult.Value.CurrentUsage,
                usageResult.Value.Limit,
                usageResult.Value.IsThresholdReached());
        }
    }

    /// <summary>Resposta do consumo registrado.</summary>
    public sealed record Response(string MetricCode, long CurrentUsage, long Limit, bool ThresholdReached);
}
