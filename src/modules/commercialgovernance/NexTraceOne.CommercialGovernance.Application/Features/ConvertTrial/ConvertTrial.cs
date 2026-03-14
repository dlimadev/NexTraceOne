using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Enums;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.ConvertTrial;

/// <summary>
/// Feature: ConvertTrial — converte um trial ativo para licença full.
///
/// Regras de negócio:
/// - Preserva dados, ativações e histórico existentes.
/// - Atualiza tipo, edição, expiração e limites.
/// - Sem necessidade de reinstalação.
/// - Trilha completa do ciclo de trial mantida.
/// - Operação irreversível (trial não pode voltar a ser trial).
/// </summary>
public static class ConvertTrial
{
    /// <summary>Comando de conversão do trial para licença full.</summary>
    public sealed record Command(
        string LicenseKey,
        LicenseEdition TargetEdition,
        int LicenseDurationDays,
        int MaxActivations,
        int GracePeriodDays = 15) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de conversão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TargetEdition).IsInEnum();
            RuleFor(x => x.LicenseDurationDays).GreaterThan(0);
            RuleFor(x => x.MaxActivations).GreaterThan(0);
            RuleFor(x => x.GracePeriodDays).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que realiza a conversão do trial.</summary>
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

            var now = dateTimeProvider.UtcNow;
            var newExpiresAt = now.AddDays(request.LicenseDurationDays);

            var convertResult = license.ConvertTrial(
                request.TargetEdition,
                newExpiresAt,
                request.MaxActivations,
                request.GracePeriodDays,
                now);

            if (convertResult.IsFailure)
            {
                return convertResult.Error;
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.Edition.ToString(),
                license.ExpiresAt,
                license.MaxActivations,
                license.GracePeriodDays);
        }
    }

    /// <summary>Resposta da conversão do trial.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string Edition,
        DateTimeOffset NewExpiresAt,
        int MaxActivations,
        int GracePeriodDays);
}
