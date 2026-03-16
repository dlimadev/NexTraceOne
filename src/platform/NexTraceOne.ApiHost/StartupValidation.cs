using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Validação de configuração crítica durante o arranque da aplicação.
/// Garante que secções obrigatórias existem antes do host aceitar tráfego.
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

        logger.LogInformation("Startup configuration validation completed successfully.");

        return app;
    }
}
