using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.ActivateLicense;

/// <summary>
/// Feature: ActivateLicense — ativa uma licença para o hardware atual.
/// </summary>
public static class ActivateLicense
{
    /// <summary>Comando de ativação da licença.</summary>
    public sealed record Command(string LicenseKey, string ActivatedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de ativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ActivatedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que ativa a licença para o hardware atual.</summary>
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
            var activationResult = license.Activate(hardwareFingerprint, request.ActivatedBy, dateTimeProvider.UtcNow);
            if (activationResult.IsFailure)
            {
                return activationResult.Error;
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                activationResult.Value.HardwareFingerprint,
                activationResult.Value.ActivatedAt);
        }
    }

    /// <summary>Resposta da ativação da licença.</summary>
    public sealed record Response(Guid LicenseId, string LicenseKey, string HardwareFingerprint, DateTimeOffset ActivatedAt);
}
