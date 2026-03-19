namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension methods para configuração de serviços no WebApplicationBuilder.
/// Centralizam responsabilidades de configuração que antes estavam no Program.cs,
/// mantendo o composition root limpo e cada configuração com responsabilidade única.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configura a política CORS para o frontend da plataforma.
    /// Valida que nenhuma origem contém wildcard (*) quando AllowCredentials está ativo,
    /// pois a especificação CORS proíbe wildcard combinado com credentials
    /// e navegadores rejeitam silenciosamente essas respostas.
    /// </summary>
    public static void AddCorsConfiguration(this WebApplicationBuilder builder)
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

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
                        "X-Tenant-Id", "X-Requested-With",
                        "X-Csrf-Token")
                    .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                    .AllowCredentials());
        });
    }
}
