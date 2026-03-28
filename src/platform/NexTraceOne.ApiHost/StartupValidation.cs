using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Security.Encryption;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Validação de configuração crítica durante o arranque da aplicação.
/// Garante que secções obrigatórias existem antes do host aceitar tráfego.
/// Em Development: regista warnings e continua com fallback inseguro quando Jwt:Secret ou a chave
/// de encriptação estão ausentes — alinhado com o comportamento do DI registration.
/// Em todos os outros ambientes: falha se Jwt:Secret ou a chave de encriptação estiverem
/// ausentes ou inválidos, e se connection strings estiverem vazias.
/// </summary>
public static class StartupValidation
{
    private static readonly string[] CriticalSections = ["ConnectionStrings", "Jwt"];
    private static readonly string[] OptionalSections = ["NexTraceOne", "Serilog", "OpenTelemetry"];

    /// <summary>
    /// Comprimento mínimo do Jwt:Secret para garantir material de chave adequado para HS256.
    /// 32 caracteres ASCII = 32 bytes de material de chave, o mínimo para HMAC-SHA256.
    /// Para alta entropia real, gerar com: openssl rand -base64 48
    /// </summary>
    private const int MinimumJwtSecretLength = 32;

    /// <summary>
    /// Valida que as secções de configuração críticas existem.
    /// Regista warnings para secções opcionais em falta.
    /// Lança <see cref="InvalidOperationException"/> se configuração crítica estiver ausente.
    /// </summary>
    public static WebApplication ValidateStartupConfiguration(this WebApplication app)
    {
        var configuration = app.Configuration;
        var logger = app.Logger;

        logger.LogInformation("Validating startup configuration...");

        var missingCritical = new List<string>();
        var validationWarnings = new List<string>();

        foreach (var section in CriticalSections)
        {
            var configSection = configuration.GetSection(section);
            if (!configSection.Exists())
            {
                missingCritical.Add(section);
                logger.LogError("Critical configuration section '{Section}' is missing.", section);
            }
        }

        foreach (var section in OptionalSections)
        {
            var configSection = configuration.GetSection(section);
            if (!configSection.Exists())
            {
                logger.LogWarning("Optional configuration section '{Section}' is missing. Some features may be limited.", section);
            }
        }

        if (missingCritical.Count > 0)
        {
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: missing critical configuration sections: {string.Join(", ", missingCritical)}. " +
                "Ensure appsettings.json and environment variables are configured correctly.");
        }

        // Validação de Jwt:Secret obrigatória em todos os ambientes
        ValidateJwtSecret(app, configuration, logger);

        // Validação de configuração OIDC
        ValidateOidcProviders(app, configuration, logger);

        // Validação de chave de encriptação obrigatória
        ValidateEncryptionKey(app, logger);

        // Validação de política de cookies seguros — proibido desabilitar fora de Development
        ValidateSecureCookiesPolicy(app, configuration, logger);

        // Validação básica de connection strings — em non-Development, connection strings vazias são fatais
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        if (connectionStrings.Exists())
        {
            foreach (var child in connectionStrings.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value))
                {
                    validationWarnings.Add($"ConnectionStrings:{child.Key}");

                    if (!app.Environment.IsDevelopment())
                    {
                        logger.LogCritical(
                            "Connection string '{Key}' is empty in non-Development environment. Aborting startup.", child.Key);
                        throw new InvalidOperationException(
                            $"NexTraceOne startup aborted: connection string '{child.Key}' must be configured " +
                            "in non-Development environments via environment variables or a secrets manager.");
                    }

                    logger.LogWarning("Connection string '{Key}' is empty.", child.Key);
                }
            }
        }

        var moduleCount = CriticalSections.Length + OptionalSections.Length;
        var validationResult = validationWarnings.Count == 0 ? "Passed" : $"Passed with {validationWarnings.Count} warning(s)";

        logger.LogInformation(
            "Startup validation summary — Environment: {Environment}, ConfigSectionsChecked: {ModuleCount}, Result: {ValidationResult}",
            app.Environment.EnvironmentName,
            moduleCount,
            validationResult);

        return app;
    }

    /// <summary>
    /// Valida a configuração do Jwt:Secret com critérios de segurança obrigatórios.
    /// Em desenvolvimento: regista warning e continua com fallback inseguro quando ausente ou curto demais,
    /// alinhado com o comportamento do DI registration que já aplica o mesmo fallback.
    /// Em todos os outros ambientes: falha se ausente, nulo, whitespace ou curto demais.
    /// A chave JWT deve ser fornecida externamente via variável de ambiente, dotnet user-secrets
    /// ou gestor de segredos — nunca hardcoded num ficheiro commitado.
    /// </summary>
    private static void ValidateJwtSecret(WebApplication app, IConfiguration configuration, ILogger logger)
    {
        var jwtSecret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Jwt:Secret is absent or empty in Development environment. " +
                    "An insecure development fallback key is in use. DO NOT use in non-development environments. " +
                    "Configure via: dotnet user-secrets set \"Jwt:Secret\" \"<key>\"");
                return;
            }

            logger.LogCritical(
                "Jwt:Secret is absent or empty in {Environment} environment. Authentication will fail. Aborting startup.",
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: Jwt:Secret must be configured in all environments. " +
                "Set the 'Jwt__Secret' environment variable, use dotnet user-secrets, or provision it via a secrets manager. " +
                $"Minimum {MinimumJwtSecretLength} characters required for HS256 key material. " +
                "Generate a strong key with: openssl rand -base64 48");
        }

        if (jwtSecret.Length < MinimumJwtSecretLength)
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Jwt:Secret is {Length} characters — below the recommended minimum of {Min} in Development environment. " +
                    "Acceptable for local development only. Use a strong key in other environments (openssl rand -base64 48).",
                    jwtSecret.Length,
                    MinimumJwtSecretLength);
                return;
            }

            logger.LogCritical(
                "Jwt:Secret is {Length} characters — below the required minimum of {Min} in {Environment} environment. Aborting startup.",
                jwtSecret.Length,
                MinimumJwtSecretLength,
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: Jwt:Secret must be at least " +
                $"{MinimumJwtSecretLength} characters long to provide adequate key material for HS256. " +
                "Provide a cryptographically strong secret via the 'Jwt__Secret' environment variable " +
                "(generate with: openssl rand -base64 48).");
        }

        logger.LogInformation(
            "Jwt:Secret validated — length {Length} characters, adequate for HS256, environment {Environment}.",
            jwtSecret.Length,
            app.Environment.EnvironmentName);
    }

    /// <summary>
    /// Valida a configuração de providers OIDC.
    /// Em Development: regista warning informativo sobre providers configurados.
    /// Em Staging/Production: regista os providers disponíveis e alerta se nenhum está configurado.
    /// Um provider é considerado configurado se tem Authority e ClientId preenchidos.
    /// </summary>
    private static void ValidateOidcProviders(WebApplication app, IConfiguration configuration, ILogger logger)
    {
        var oidcSection = configuration.GetSection("OidcProviders");
        if (!oidcSection.Exists())
        {
            logger.LogInformation("OIDC providers section not found. Federated authentication is disabled.");
            return;
        }

        var configuredProviders = new List<string>();
        var incompleteProviders = new List<string>();

        foreach (var providerSection in oidcSection.GetChildren())
        {
            var providerName = providerSection.Key;
            var authority = providerSection["Authority"];
            var clientId = providerSection["ClientId"];
            var clientSecret = providerSection["ClientSecret"];

            if (!string.IsNullOrWhiteSpace(authority) &&
                !string.IsNullOrWhiteSpace(clientId) &&
                !string.IsNullOrWhiteSpace(clientSecret) &&
                !authority.Contains("{tenant-id}"))
            {
                configuredProviders.Add(providerName);
            }
            else if (!string.IsNullOrWhiteSpace(clientId) || !string.IsNullOrWhiteSpace(authority))
            {
                incompleteProviders.Add(providerName);
            }
        }

        if (configuredProviders.Count > 0)
        {
            logger.LogInformation(
                "OIDC providers configured: {Providers}. Federated authentication is available.",
                string.Join(", ", configuredProviders));
        }

        if (incompleteProviders.Count > 0)
        {
            logger.LogWarning(
                "OIDC providers with incomplete configuration: {Providers}. " +
                "Ensure Authority, ClientId and ClientSecret are set for each active provider.",
                string.Join(", ", incompleteProviders));
        }

        if (configuredProviders.Count == 0 && !app.Environment.IsDevelopment())
        {
            logger.LogWarning(
                "No OIDC providers are fully configured in {Environment}. " +
                "Enterprise SSO (federated authentication) will not be available. " +
                "Configure at least one provider in the OidcProviders section.",
                app.Environment.EnvironmentName);
        }
    }

    /// <summary>
    /// Valida a configuração de chave de encriptação AES obrigatória.
    /// Em desenvolvimento: regista warning e continua com fallback inseguro quando ausente.
    /// Em todos os outros ambientes: falha se ausente, nula, whitespace ou inválida.
    /// A variável de ambiente esperada é <c>NEXTRACE_ENCRYPTION_KEY</c>.
    /// O valor deve ser um Base64-encoded 32-byte key ou uma string UTF-8 com 32 caracteres.
    /// </summary>
    private static void ValidateEncryptionKey(WebApplication app, ILogger logger)
    {
        try
        {
            EncryptionKeyMaterial.ValidateRequiredEnvironmentVariable(app.Environment.IsDevelopment());
            logger.LogInformation(
                "{VariableName} validated in {Environment} environment.",
                EncryptionKeyMaterial.EnvironmentVariableName,
                app.Environment.EnvironmentName);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogCritical(
                "{VariableName} is invalid in {Environment} environment. Aborting startup.",
                EncryptionKeyMaterial.EnvironmentVariableName,
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: {ex.Message} " +
                "Set it via environment variable, .env, dotnet user-secrets, or a secrets manager.",
                ex);
        }
    }

    /// <summary>
    /// Valida a política de cookies seguros por ambiente.
    /// Em Development: permite RequireSecureCookies=false e regista um aviso explícito.
    /// Em qualquer outro ambiente (Staging, Production, etc.): falha o startup se RequireSecureCookies for false,
    /// porque cookies inseguros em produção expõem tokens de sessão a intercepção via HTTP.
    /// </summary>
    private static void ValidateSecureCookiesPolicy(WebApplication app, IConfiguration configuration, ILogger logger)
    {
        var rawValue = configuration["Auth:CookieSession:RequireSecureCookies"];

        // Absent or any value other than an explicit "false" is treated as secure (safe default).
        var isExplicitlyInsecure = string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase);

        if (isExplicitlyInsecure && !app.Environment.IsDevelopment())
        {
            logger.LogCritical(
                "Auth:CookieSession:RequireSecureCookies is false in {Environment} environment. " +
                "Insecure cookies expose session tokens to interception. Aborting startup.",
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: Auth:CookieSession:RequireSecureCookies must be true " +
                $"in non-Development environments (current: {app.Environment.EnvironmentName}). " +
                "Set 'Auth__CookieSession__RequireSecureCookies=true' via environment variable or configuration. " +
                "RequireSecureCookies=false is only permitted in local Development.");
        }

        if (isExplicitlyInsecure)
        {
            logger.LogWarning(
                "Auth:CookieSession:RequireSecureCookies is false in Development environment. " +
                "This is acceptable for local HTTP development but must NEVER be used in staging or production.");
        }
        else
        {
            logger.LogInformation(
                "Auth:CookieSession:RequireSecureCookies validated — secure cookies enforced in {Environment} environment.",
                app.Environment.EnvironmentName);
        }
    }
}
