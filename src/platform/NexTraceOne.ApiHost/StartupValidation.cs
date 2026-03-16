using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Validação de configuração crítica durante o arranque da aplicação.
/// Garante que secções obrigatórias existem antes do host aceitar tráfego.
/// Em ambientes não-Development, valida também que Jwt:Secret está preenchido
/// e que connection strings não estão vazias.
/// </summary>
public static class StartupValidation
{
    private static readonly string[] CriticalSections = ["ConnectionStrings", "Jwt"];
    private static readonly string[] OptionalSections = ["NexTraceOne", "Serilog", "OpenTelemetry"];

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
        if (!app.Environment.IsDevelopment())
        {
            var jwtSecret = configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                validationWarnings.Add("Jwt:Secret");
                logger.LogWarning("Jwt:Secret is empty in non-Development environment. Authentication may fail.");
            }
        }

        // Validação básica de connection strings — apenas verifica se não estão vazias
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        if (connectionStrings.Exists())
        {
            foreach (var child in connectionStrings.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value))
                {
                    validationWarnings.Add($"ConnectionStrings:{child.Key}");
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
}
