using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.CheckCapability;

/// <summary>
/// Feature: CheckCapability — verifica se a licença possui uma capability habilitada.
/// </summary>
public static class CheckCapability
{
    /// <summary>Query de verificação de capability da licença.</summary>
    public sealed record Query(string LicenseKey, string CapabilityCode) : IQuery<Response>;

    /// <summary>Valida a entrada da verificação de capability.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.CapabilityCode).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que verifica a capability licenciada para o hardware atual.</summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IHardwareFingerprintProvider hardwareFingerprintProvider,
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

            var hardwareFingerprint = hardwareFingerprintProvider.Generate();
            var verificationResult = license.VerifyAt(dateTimeProvider.UtcNow, hardwareFingerprint);
            if (verificationResult.IsFailure)
            {
                return verificationResult.Error;
            }

            var capabilityResult = license.CheckCapability(request.CapabilityCode, dateTimeProvider.UtcNow);
            if (capabilityResult.IsFailure)
            {
                return capabilityResult.Error;
            }

            return new Response(capabilityResult.Value.Code, capabilityResult.Value.Name, capabilityResult.Value.IsEnabled);
        }
    }

    /// <summary>Resposta da verificação de capability.</summary>
    public sealed record Response(string Code, string Name, bool IsEnabled);
}
