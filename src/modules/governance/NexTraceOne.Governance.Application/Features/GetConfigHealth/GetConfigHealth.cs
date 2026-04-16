using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetConfigHealth;

/// <summary>
/// Feature: GetConfigHealth — validação da saúde da configuração da plataforma.
/// Verifica todas as variáveis obrigatórias e opcionais com sugestões de resolução.
/// Retorna estado geral: "ok", "warning" ou "degraded".
/// Nunca expõe valores de segredos — apenas booleanos e metadados não sensíveis.
/// </summary>
public static class GetConfigHealth
{
    /// <summary>Query sem parâmetros — verifica configuração no momento do request.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que valida configuração runtime via IConfiguration.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var checks = new List<ConfigCheckDto>();

            // ─── Obrigatórios ─────────────────────────────────────────────────
            checks.Add(CheckJwtSecret());
            checks.Add(CheckConnectionStrings());
            checks.Add(CheckEncryptionKey());

            // ─── Opcionais ────────────────────────────────────────────────────
            checks.Add(CheckSmtp());
            checks.Add(CheckOllama());
            checks.Add(CheckOpenAi());
            checks.Add(CheckOtelCollector());
            checks.Add(CheckCorsOrigins());
            checks.Add(CheckSerilog());

            var hasDegraded = checks.Any(c => c.Status == "degraded");
            var hasWarning = checks.Any(c => c.Status == "warning");
            var overallStatus = hasDegraded ? "degraded" : hasWarning ? "warning" : "ok";

            var response = new Response(
                Status: overallStatus,
                Checks: checks,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }

        // ─── Individual checks ───────────────────────────────────────────────

        private ConfigCheckDto CheckJwtSecret()
        {
            const string key = "Jwt__Secret";
            var value = configuration["Jwt:Secret"];

            if (string.IsNullOrWhiteSpace(value))
                return new ConfigCheckDto(key, "degraded",
                    "JWT Secret is missing — authentication will fail.",
                    "Set Jwt__Secret via environment variable. Generate with: openssl rand -base64 48");

            if (value.Length < 32)
                return new ConfigCheckDto(key, "warning",
                    $"JWT Secret is {value.Length} characters — recommended minimum is 32.",
                    "Increase Jwt__Secret length for stronger security (openssl rand -base64 48).");

            return new ConfigCheckDto(key, "ok", $"JWT Secret configured — {value.Length} characters.");
        }

        private ConfigCheckDto CheckConnectionStrings()
        {
            const string key = "ConnectionStrings";
            var section = configuration.GetSection("ConnectionStrings");

            if (!section.Exists())
                return new ConfigCheckDto(key, "degraded",
                    "ConnectionStrings section is missing.",
                    "Add ConnectionStrings section to appsettings.json or set ConnectionStrings__ environment variables.");

            var configured = section.GetChildren().Count(c => !string.IsNullOrWhiteSpace(c.Value));
            var total = section.GetChildren().Count();

            if (configured == 0)
                return new ConfigCheckDto(key, "degraded",
                    "No connection strings are configured.",
                    "Configure ConnectionStrings__NexTraceOne at minimum.");

            if (configured < total)
                return new ConfigCheckDto(key, "warning",
                    $"{configured}/{total} connection strings are configured. {total - configured} are empty.",
                    "Configure all required connection strings for full functionality.");

            return new ConfigCheckDto(key, "ok", $"All {configured} connection strings configured.");
        }

        private ConfigCheckDto CheckEncryptionKey()
        {
            const string key = "NEXTRACE_ENCRYPTION_KEY";
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(value))
                return new ConfigCheckDto(key, "degraded",
                    "Encryption key environment variable is not set.",
                    $"Set the {key} environment variable (Base64-encoded 32-byte AES key).");

            return new ConfigCheckDto(key, "ok", "Encryption key is set.");
        }

        private ConfigCheckDto CheckSmtp()
        {
            const string key = "Smtp__Host";
            var host = configuration["Smtp:Host"];

            return string.IsNullOrWhiteSpace(host)
                ? new ConfigCheckDto(key, "warning",
                    "SMTP not configured — email notifications are disabled.",
                    "Set Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password, Smtp__From to enable email.")
                : new ConfigCheckDto(key, "ok", $"SMTP configured — host: {host}.");
        }

        private ConfigCheckDto CheckOllama()
        {
            const string key = "AiRuntime__Ollama__BaseUrl";
            var ollamaEnabled = configuration.GetValue<bool?>("AiRuntime:Ollama:Enabled") ?? true;
            var baseUrl = configuration["AiRuntime:Ollama:BaseUrl"] ?? "http://localhost:11434";

            if (!ollamaEnabled)
                return new ConfigCheckDto(key, "ok", "Ollama disabled by configuration.");

            return new ConfigCheckDto(key, "ok",
                $"Ollama enabled — base URL: {baseUrl}. Connectivity verified at startup by Preflight.");
        }

        private ConfigCheckDto CheckOpenAi()
        {
            const string key = "AiRuntime__OpenAI__Enabled";
            var enabled = configuration.GetValue<bool?>("AiRuntime:OpenAI:Enabled") ?? false;
            var apiKey = configuration["AiRuntime:OpenAI:ApiKey"];

            if (!enabled)
                return new ConfigCheckDto(key, "ok", "OpenAI disabled by configuration (using local AI).");

            if (string.IsNullOrWhiteSpace(apiKey))
                return new ConfigCheckDto(key, "warning",
                    "OpenAI is enabled but API key is not set.",
                    "Set AiRuntime__OpenAI__ApiKey or disable OpenAI with AiRuntime__OpenAI__Enabled=false.");

            return new ConfigCheckDto(key, "ok", "OpenAI enabled and API key is set.");
        }

        private ConfigCheckDto CheckOtelCollector()
        {
            const string key = "OpenTelemetry__Endpoint";
            var endpoint = configuration["OpenTelemetry:Endpoint"] ?? configuration["Telemetry:Endpoint"];
            var enabled = configuration.GetValue<bool?>("OpenTelemetry:Enabled") ?? true;

            if (!enabled)
                return new ConfigCheckDto(key, "ok", "OpenTelemetry disabled by configuration.");

            return string.IsNullOrWhiteSpace(endpoint)
                ? new ConfigCheckDto(key, "warning",
                    "OTel Collector endpoint not configured — distributed tracing disabled.",
                    "Set OpenTelemetry__Endpoint (e.g., http://collector:4317) to enable tracing.")
                : new ConfigCheckDto(key, "ok", $"OTel Collector endpoint: {endpoint}.");
        }

        private ConfigCheckDto CheckCorsOrigins()
        {
            const string key = "Cors__AllowedOrigins";
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

            return origins.Length == 0
                ? new ConfigCheckDto(key, "warning",
                    "No CORS origins configured — browser clients from external domains will be blocked.",
                    "Set Cors__AllowedOrigins with the frontend URL (e.g., https://nextraceone.acme.com).")
                : new ConfigCheckDto(key, "ok", $"CORS configured — {origins.Length} origin(s).");
        }

        private ConfigCheckDto CheckSerilog()
        {
            const string key = "Serilog";
            var section = configuration.GetSection("Serilog");

            return !section.Exists()
                ? new ConfigCheckDto(key, "warning",
                    "Serilog configuration not found — using default logging.",
                    "Add Serilog section to appsettings.json for structured logging.")
                : new ConfigCheckDto(key, "ok", "Serilog configuration present.");
        }
    }

    /// <summary>Resposta de saúde da configuração da plataforma.</summary>
    public sealed record Response(
        string Status,
        IReadOnlyList<ConfigCheckDto> Checks,
        DateTimeOffset GeneratedAt);

    /// <summary>Resultado individual de um check de configuração.</summary>
    /// <param name="Key">Chave de configuração ou nome do check.</param>
    /// <param name="Status">"ok", "warning" ou "degraded".</param>
    /// <param name="Message">Mensagem legível com o estado do check.</param>
    /// <param name="Suggestion">Sugestão de resolução. Nulo quando estado é ok.</param>
    public sealed record ConfigCheckDto(
        string Key,
        string Status,
        string Message,
        string? Suggestion = null);
}
