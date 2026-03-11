using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.VerifyLicenseOnStartup;

/// <summary>
/// Feature: VerifyLicenseOnStartup — valida a licença e o hardware no boot da aplicação.
/// </summary>
public static class VerifyLicenseOnStartup
{
    /// <summary>Query de verificação da licença no startup.</summary>
    public sealed record Query(string LicenseKey) : IQuery<Response>;

    /// <summary>Valida a entrada da verificação de licença.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que valida a licença contra o hardware atual.</summary>
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

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName,
                license.ExpiresAt,
                hardwareFingerprint,
                true);
        }
    }

    /// <summary>Resposta da validação da licença no startup.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName,
        DateTimeOffset ExpiresAt,
        string HardwareFingerprint,
        bool IsValid);
}
