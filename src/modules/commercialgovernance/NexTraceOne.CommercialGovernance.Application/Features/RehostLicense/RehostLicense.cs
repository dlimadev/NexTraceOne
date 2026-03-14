using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.RehostLicense;

/// <summary>
/// Feature: RehostLicense — operação de vendor ops para rehost de licença.
///
/// Remove o hardware binding atual para permitir ativação em novo hardware.
/// Usada em cenários de migração de servidor, substituição de hardware
/// ou reorganização de infraestrutura do cliente.
///
/// Permissão requerida: licensing:vendor:license:rehost
/// </summary>
public static class RehostLicense
{
    /// <summary>Comando para rehost de uma licença.</summary>
    public sealed record Command(string LicenseKey) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de rehost.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que remove o hardware binding da licença para permitir reativação.
    /// Preserva todo o histórico de ativações para auditoria.
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var license = await licenseRepository.GetByLicenseKeyAsync(request.LicenseKey, cancellationToken);
            if (license is null)
            {
                return LicensingErrors.LicenseKeyNotFound(request.LicenseKey);
            }

            var result = license.Rehost();
            if (result.IsFailure)
            {
                return result.Error;
            }

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName);
        }
    }

    /// <summary>Resposta do rehost de licença.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName);
}
