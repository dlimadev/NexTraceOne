namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension methods para configuração de serviços no WebApplicationBuilder.
/// Centralizam responsabilidades de configuração que antes estavam no Program.cs,
/// mantendo o composition root limpo e cada configuração com responsabilidade única.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configura a política CORS para o frontend da plataforma, com validação por ambiente.
    ///
    /// Em Development/CI: aceita origens por defeito (localhost:5173, localhost:3000)
    /// quando nenhuma configuração explícita é fornecida.
    ///
    /// Em Staging/Production: exige que Cors:AllowedOrigins esteja explicitamente
    /// configurado — não permite fallback para origens de desenvolvimento.
    ///
    /// Valida que nenhuma origem contém wildcard (*) quando AllowCredentials está ativo,
    /// pois a especificação CORS proíbe wildcard combinado com credentials
    /// e navegadores rejeitam silenciosamente essas respostas.
    /// </summary>
    public static void AddCorsConfiguration(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        // Em ambientes não-desenvolvimento, origens CORS devem ser configuradas explicitamente.
        var isNonDevelopmentEnvironment = !string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(environment, "CI", StringComparison.OrdinalIgnoreCase);

        if (isNonDevelopmentEnvironment && (configuredOrigins is null || configuredOrigins.Length == 0))
        {
            throw new InvalidOperationException(
                $"CORS origins must be explicitly configured in '{environment}' environment. " +
                "Set 'Cors:AllowedOrigins' in appsettings or via environment variable 'Cors__AllowedOrigins__0', etc. " +
                "Falling back to localhost defaults is not allowed in non-development environments.");
        }

        var corsOrigins = configuredOrigins ?? ["http://localhost:5173", "http://localhost:3000"];

        foreach (var origin in corsOrigins)
        {
            if (origin.Contains('*'))
            {
                throw new InvalidOperationException(
                    $"CORS origin '{origin}' contains a wildcard. Wildcards are not allowed when AllowCredentials is enabled. " +
                    "Configure explicit origins in 'Cors:AllowedOrigins'.");
            }
        }

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(corsOrigins)
                    .WithHeaders(
                        "Content-Type", "Authorization",
                        "X-Tenant-Id", "X-Environment-Id", "X-Correlation-Id",
                        "X-Requested-With",
                        "X-Csrf-Token")
                    .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                    .AllowCredentials());
        });
    }
}
