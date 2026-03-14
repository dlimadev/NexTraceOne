using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Errors;

namespace NexTraceOne.Licensing.Application.Features.RevokeLicense;

/// <summary>
/// Feature: RevokeLicense — operação de vendor ops para revogar uma licença permanentemente.
///
/// Usada pelo backoffice interno da NexTraceOne quando uma licença precisa ser
/// desativada definitivamente (cancelamento, fraude, violação de termos).
///
/// Permissão requerida: licensing:vendor:license:revoke
/// </summary>
public static class RevokeLicense
{
    /// <summary>Comando para revogar uma licença.</summary>
    public sealed record Command(string LicenseKey) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de revogação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que revoga uma licença permanentemente.
    /// Marca a licença como revogada e impede qualquer operação futura.
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

            if (!license.IsActive)
            {
                return LicensingErrors.LicenseAlreadyRevoked();
            }

            license.Revoke();

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName);
        }
    }

    /// <summary>Resposta da revogação de licença.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName);
}
