using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.StartSamlLogin;

/// <summary>
/// Feature: StartSamlLogin — inicia o fluxo de autenticação SAML 2.0.
///
/// Gera um AuthnRequest único e retorna a URL de redirect para o IdP.
/// O RequestId deve ser preservado pelo cliente para validação futura da response.
///
/// Fluxo:
/// 1. Frontend chama GET /auth/saml/sso com ReturnUrl opcional.
/// 2. Este handler verifica config SAML, gera RequestId único e constrói a AuthnRequest URL.
/// 3. Frontend (ou API) redireciona o browser para a URL retornada.
/// 4. IdP autentica o utilizador e POST para /auth/saml/acs com SAMLResponse.
/// </summary>
public static class StartSamlLogin
{
    /// <summary>Query para iniciar o fluxo SAML.</summary>
    public sealed record Query(string? ReturnUrl = null) : IQuery<Response>, IPublicRequest;

    /// <summary>Resposta com URL de redirect para o IdP e identificador da request.</summary>
    public sealed record Response(
        /// <summary>URL completa de redirect para o IdP com AuthnRequest encoded.</summary>
        string RedirectUrl,
        /// <summary>Identificador único da AuthnRequest (para correlação com a SAMLResponse).</summary>
        string RequestId);

    /// <summary>Validação do query — sem restrições adicionais.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() { }
    }

    /// <summary>
    /// Handler que constrói a URL de redirect SAML.
    /// Usa ISamlConfigProvider para obter a config activa e ISamlService para construir o AuthnRequest.
    /// </summary>
    public sealed class Handler(
        ISamlConfigProvider configProvider,
        ISamlService samlService,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        /// <summary>URL do Assertion Consumer Service do SP.</summary>
        private const string AcsPath = "/auth/saml/acs";

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var config = await configProvider.GetActiveConfigAsync(cancellationToken);

            if (config is null)
            {
                return Error.Validation(
                    "Identity.Saml.NotConfigured",
                    "saml.not_configured");
            }

            var requestId = $"_{Guid.NewGuid():N}";
            var relayState = request.ReturnUrl ?? "/";

            var redirectUrl = samlService.BuildAuthnRequestUrl(
                config.SsoUrl,
                config.EntityId,
                AcsPath,
                requestId,
                relayState);

            return new Response(redirectUrl, requestId);
        }
    }
}
