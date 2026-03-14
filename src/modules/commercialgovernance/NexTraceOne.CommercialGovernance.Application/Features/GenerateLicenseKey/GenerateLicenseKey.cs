using System.Security.Cryptography;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CommercialCatalog.Domain.Errors;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.CommercialCatalog.Application.Features.GenerateLicenseKey;

/// <summary>
/// Feature: GenerateLicenseKey — operação de vendor ops para gerar uma nova
/// chave criptograficamente segura para uma licença existente.
///
/// Gera uma chave no formato "NXKEY-XXXX-XXXX-XXXX-XXXX" usando bytes aleatórios
/// criptograficamente seguros. Não altera a chave da licença no aggregate —
/// a chave gerada é retornada para uso externo (ex: envio por e-mail ao cliente).
///
/// Permissão requerida: licensing:vendor:license:manage
/// </summary>
public static class GenerateLicenseKey
{
    /// <summary>Comando para geração de chave de licença.</summary>
    public sealed record Command(
        Guid LicenseId,
        string? KeyFormat = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de geração de chave.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseId).NotEmpty();
            RuleFor(x => x.KeyFormat)
                .MaximumLength(50)
                .When(x => x.KeyFormat is not null);
        }
    }

    /// <summary>
    /// Handler que verifica existência da licença e gera uma chave
    /// criptograficamente segura.
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var licenseId = LicenseId.From(request.LicenseId);
            var license = await licenseRepository.GetByIdAsync(licenseId, cancellationToken);

            if (license is null)
            {
                return CommercialCatalogErrors.LicenseNotFoundForKeyGeneration(request.LicenseId);
            }

            var newKey = GenerateSecureKey();

            return new Response(license.Id.Value, newKey);
        }

        /// <summary>
        /// Gera uma chave de licença criptograficamente segura no formato NXKEY-XXXX-XXXX-XXXX-XXXX.
        /// Utiliza RandomNumberGenerator para garantir entropia adequada.
        /// </summary>
        private static string GenerateSecureKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(16);
            var hex = Convert.ToHexString(bytes);

            return $"NXKEY-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
        }
    }

    /// <summary>Resposta da geração de chave de licença.</summary>
    public sealed record Response(Guid LicenseId, string NewLicenseKey);
}
