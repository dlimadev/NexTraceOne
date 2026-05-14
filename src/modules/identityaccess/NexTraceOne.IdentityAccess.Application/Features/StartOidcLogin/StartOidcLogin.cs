using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.StartOidcLogin;

/// <summary>
/// Feature: StartOidcLogin — inicia o fluxo de autenticação OIDC.
///
/// Gera um nonce de estado (state) para prevenção de CSRF e retorna
/// a URL de redirect para o browser iniciar o fluxo com o provider externo.
///
/// Fluxo:
/// 1. Frontend chama POST /auth/oidc/start com provider e returnTo.
/// 2. Este handler gera state = Base64(nonce:returnTo) e constrói a authorization URL.
/// 3. Frontend redireciona o browser para a URL retornada.
/// 4. Provider autentica e redireciona para /auth/oidc/callback?code=...&amp;state=...
///
/// Deep Link Preservation:
/// O parâmetro returnTo (URL original do usuário antes do login) é embutido no state
/// de forma segura. No callback, o state é validado e o returnTo é extraído para
/// restaurar a navegação após autenticação bem-sucedida.
///
/// Segurança:
/// - state é um nonce Base64(UUID:returnTo) gerado pelo servidor.
/// - returnTo é validado contra lista de origens permitidas para prevenir open redirect.
/// - O state é vinculado à sessão do browser via cookie seguro no middleware.
/// </summary>
public static class StartOidcLogin
{
    /// <summary>Comando para iniciar o fluxo OIDC.</summary>
    public sealed record Command(
        string Provider,
        string? ReturnTo = null) : ICommand<Response>, IPublicRequest;

    /// <summary>Resposta com URL de redirect e state para o frontend.</summary>
    public sealed record Response(
        /// <summary>URL completa para redirect ao provider OIDC.</summary>
        string AuthorizationUrl,

        /// <summary>
        /// State opaco gerado pelo servidor.
        /// Deve ser preservado pelo frontend para validação no callback.
        /// Já está incluído na AuthorizationUrl, exposto aqui para diagnóstico.
        /// </summary>
        string State);

    /// <summary>Valida o comando de início de fluxo OIDC.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        /// <summary>
        /// Lista de prefixos permitidos para returnTo.
        /// Previne open redirect para domínios externos maliciosos.
        /// Configurável via appsettings em implementação futura.
        /// </summary>
        private static readonly string[] AllowedReturnToPrefixes = ["/", "http://localhost", "https://localhost"];

        public Validator()
        {
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ReturnTo)
                .Must(BeASafeReturnTo)
                .When(x => x.ReturnTo is not null)
                .WithMessage("The returnTo URL must be a relative path or a localhost URL.");
        }

        /// <summary>
        /// Valida que o destino de retorno é seguro.
        /// Previne open redirect para domínios externos.
        /// </summary>
        private static bool BeASafeReturnTo(string? returnTo)
        {
            if (string.IsNullOrWhiteSpace(returnTo))
                return true;

            // Rejeita URLs com protocolo duplo (//evil.com)
            if (returnTo.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                return false;

            return AllowedReturnToPrefixes.Any(prefix =>
                returnTo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Handler que valida o provider, gera o state e constrói a authorization URL.
    /// O returnTo é codificado no state para ser recuperado no callback.
    /// </summary>
    public sealed class Handler(
        IOidcProvider oidcProvider,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider dateTimeProvider,
        NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        /// <summary>URI de callback registrado. Em produção, carregado de configuração.</summary>
        private const string CallbackPath = "/api/v1/identity/auth/oidc/callback";

        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!oidcProvider.IsConfigured(request.Provider))
            {
                return Task.FromResult<Result<Response>>(
                    IdentityErrors.OidcProviderNotConfigured(request.Provider));
            }

            // Gera nonce único para prevenção de CSRF
            var nonce = Guid.NewGuid().ToString("N");

            // Codifica o returnTo no state para recuperação no callback
            // Formato: "{nonce}:{returnTo}" onde returnTo é URL-encoded
            var returnTo = request.ReturnTo ?? "/";
            var statePayload = $"{nonce}:{Uri.EscapeDataString(returnTo)}";
            var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(statePayload));

            var redirectUri = BuildCallbackUri();
            var authorizationUrl = oidcProvider.BuildAuthorizationUrl(request.Provider, state, redirectUri);

            // Registra evento de início de fluxo OIDC para rastreabilidade
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            var securityEvent = SecurityEvent.Create(
                tenantId,
                userId: null,
                sessionId: null,
                SecurityEventType.OidcFlowStarted,
                $"OIDC login flow started for provider '{request.Provider}'.",
                riskScore: 0,
                ipAddress: null,
                userAgent: null,
                $"{{\"provider\":\"{request.Provider}\",\"nonce\":\"{nonce}\"}}",
                dateTimeProvider.UtcNow);
            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Task.FromResult(Result<Response>.Success(new Response(authorizationUrl, state)));
        }

        /// <summary>
        /// Constrói a URI de callback completa.
        /// Em produção, deve ser baseada em configuração (AppSettings:BaseUrl).
        /// </summary>
        private static string BuildCallbackUri()
            => CallbackPath;
    }
}
