using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;
using FederatedLoginFeature = NexTraceOne.IdentityAccess.Application.Features.FederatedLogin.FederatedLogin;

namespace NexTraceOne.IdentityAccess.Application.Features.SamlAcsCallback;

/// <summary>
/// Feature: SamlAcsCallback — processa a SAMLResponse recebida do IdP no Assertion Consumer Service.
///
/// Fluxo:
/// 1. IdP faz POST para /auth/saml/acs com SAMLResponse (base64) e RelayState.
/// 2. Este handler valida e parseia a SAMLResponse via ISamlService.
/// 3. Delega autenticação federada para FederatedLogin.Command (cria/vincula utilizador, cria sessão).
/// 4. Retorna tokens de acesso + URL de destino (RelayState).
/// </summary>
public static class SamlAcsCallback
{
    /// <summary>Comando do ACS callback com a SAMLResponse recebida do IdP.</summary>
    public sealed record Command(
        string SamlResponse,
        string? RelayState = null,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<Response>, IPublicRequest;

    /// <summary>Resposta com tokens e URL de destino pós-autenticação SAML.</summary>
    public sealed record Response(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        LocalLoginFeature.UserResponse User,
        string ReturnTo);

    /// <summary>Valida que a SAMLResponse não está vazia.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SamlResponse).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que valida a SAMLResponse e autentica o utilizador via FederatedLogin.
    ///
    /// Usa ISamlConfigProvider para obter o certificado IdP e ISamlService para validar a assinatura.
    /// Delega criação/provisioning do utilizador ao FederatedLogin.Command via ISender.
    /// </summary>
    public sealed class Handler(
        ISamlConfigProvider configProvider,
        ISamlService samlService,
        ISender mediator) : ICommandHandler<Command, Response>
    {
        /// <summary>Provider name usado ao registar identidades SAML.</summary>
        private const string SamlProvider = "saml";

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var config = await configProvider.GetActiveConfigAsync(cancellationToken);

            if (config is null)
            {
                return Error.Validation(
                    "Identity.Saml.NotConfigured",
                    "saml.not_configured");
            }

            SamlParsedAssertion assertion;
            try
            {
                assertion = samlService.ParseSamlResponse(request.SamlResponse, config.IdpCertificate);
            }
            catch (Exception ex)
            {
                return Error.Validation(
                    "Identity.Saml.InvalidResponse",
                    $"SAML response validation failed: {ex.Message}");
            }

            var federatedCommand = new FederatedLoginFeature.Command(
                Provider: SamlProvider,
                ExternalId: assertion.NameId,
                Email: assertion.Email,
                Name: assertion.Name ?? assertion.Email,
                IpAddress: request.IpAddress,
                UserAgent: request.UserAgent,
                Groups: assertion.Groups);

            var loginResult = await mediator.Send(federatedCommand, cancellationToken);

            if (loginResult.IsFailure)
            {
                return loginResult.Error;
            }

            var returnTo = string.IsNullOrWhiteSpace(request.RelayState) ? "/" : request.RelayState;

            return new Response(
                loginResult.Value.AccessToken,
                loginResult.Value.RefreshToken,
                loginResult.Value.ExpiresIn,
                loginResult.Value.User,
                returnTo);
        }
    }
}
