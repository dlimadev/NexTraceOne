using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Validação de configuração crítica durante o arranque da aplicação.
/// Garante que secções obrigatórias existem antes do host aceitar tráfego.
/// Em ambientes não-Development, valida também que Jwt:Secret está preenchido
/// e tem comprimento mínimo para operação segura com HS256 (32 caracteres = 32 bytes de material de chave),
/// e que connection strings não estão vazias.
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

        // Validação de Jwt:Secret em ambientes não-Development
        ValidateJwtSecret(app, configuration, logger);

        // Validação de configuração OIDC
        ValidateOidcProviders(app, configuration, logger);

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
    /// Valida a configuração do Jwt:Secret com critérios de segurança por ambiente.
    /// Em Development: avisa se ausente, mas permite continuar com placeholder.
    /// Em Staging/Production: falha se ausente, nulo, whitespace ou curto demais.
    /// </summary>
    private static void ValidateJwtSecret(WebApplication app, IConfiguration configuration, ILogger logger)
    {
        var jwtSecret = configuration["Jwt:Secret"];

        if (app.Environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                logger.LogWarning(
                    "Jwt:Secret is not configured in Development environment. " +
                    "Set 'Jwt:Secret' in appsettings.Development.json or via dotnet user-secrets for local use. " +
                    "This would block startup in non-Development environments.");
            }
            else if (jwtSecret.Length < MinimumJwtSecretLength)
            {
                logger.LogWarning(
                    "Jwt:Secret is {Length} characters in Development — below the recommended minimum of {Min}. " +
                    "Use a secret with at least {Min} characters for adequate key material in HS256.",
                    jwtSecret.Length,
                    MinimumJwtSecretLength);
            }

            return;
        }

        // Staging / Production: secret must be present, non-whitespace and meet minimum length
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            logger.LogCritical(
                "Jwt:Secret is absent or empty in {Environment} environment. Authentication will fail. Aborting startup.",
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: Jwt:Secret must be configured in {app.Environment.EnvironmentName} environments. " +
                "Set the 'Jwt__Secret' environment variable or provision it via a secrets manager " +
                $"(minimum {MinimumJwtSecretLength} characters required for HS256 key material).");
        }

        if (jwtSecret.Length < MinimumJwtSecretLength)
        {
            logger.LogCritical(
                "Jwt:Secret is {Length} characters — below the required minimum of {Min} in {Environment} environment. Aborting startup.",
                jwtSecret.Length,
                MinimumJwtSecretLength,
                app.Environment.EnvironmentName);
            throw new InvalidOperationException(
                $"NexTraceOne startup aborted: Jwt:Secret in {app.Environment.EnvironmentName} must be at least " +
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
}
