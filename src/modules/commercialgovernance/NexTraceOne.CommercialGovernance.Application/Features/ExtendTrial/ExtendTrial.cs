using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.ExtendTrial;

/// <summary>
/// Feature: ExtendTrial — estende o período de trial por dias adicionais.
///
/// Regras de negócio:
/// - Máximo de 1 extensão permitida.
/// - Extensão padrão: 15 dias (configurável no comando).
/// - Requer que a licença seja do tipo Trial e esteja ativa.
/// - Operação auditável — extensões são rastreáveis.
/// </summary>
public static class ExtendTrial
{
    /// <summary>Comando para estender o trial.</summary>
    public sealed record Command(string LicenseKey, int AdditionalDays = 15) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de extensão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AdditionalDays).InclusiveBetween(1, 30);
        }
    }

    /// <summary>Handler que estende o período de trial.</summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
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

            var result = license.ExtendTrial(request.AdditionalDays, dateTimeProvider.UtcNow);
            if (result.IsFailure)
            {
                return result.Error;
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.ExpiresAt,
                license.TrialExtensionCount);
        }
    }

    /// <summary>Resposta da extensão do trial.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        DateTimeOffset NewExpiresAt,
        int ExtensionCount);
}
