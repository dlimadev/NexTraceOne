using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexTraceOne.CLI.Services;

/// <summary>
/// Gestão de configuração local do CLI NEX.
/// Persiste a configuração em ~/.nex/config.json.
/// </summary>
public sealed class CliConfig
{
    private static readonly string ConfigDir =
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nex");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>URL base do servidor NexTraceOne.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Token de autenticação API.</summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>Ambiente padrão (ex: production, staging, development).</summary>
    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    /// <summary>Persona padrão do utilizador (ex: Engineer, TechLead, Architect).</summary>
    [JsonPropertyName("persona")]
    public string? Persona { get; set; }

    // ── Static helpers ─────────────────────────────────────────────────────────

    /// <summary>Carrega a configuração do disco. Retorna instância vazia se não existir.</summary>
    public static CliConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new CliConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<CliConfig>(json, JsonOptions) ?? new CliConfig();
        }
        catch
        {
            return new CliConfig();
        }
    }

    /// <summary>Persiste a configuração no disco.</summary>
    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>Resolve a URL: usa o valor fornecido explicitamente, depois config, depois env var, depois default.</summary>
    public static string ResolveUrl(string? explicitValue)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
            return explicitValue;

        var fromEnv = System.Environment.GetEnvironmentVariable("NEX_API_URL");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var config = Load();
        if (!string.IsNullOrWhiteSpace(config.Url))
            return config.Url;

        return "http://localhost:8080";
    }

    /// <summary>Resolve o token: usa o valor fornecido explicitamente, depois env var, depois config.</summary>
    public static string? ResolveToken(string? explicitValue)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
            return explicitValue;

        var fromEnv = System.Environment.GetEnvironmentVariable("NEXTRACE_TOKEN");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        return Load().Token;
    }

    /// <summary>Resolve o ambiente: usa o valor fornecido explicitamente, depois env var, depois config.</summary>
    public static string? ResolveEnvironment(string? explicitValue)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
            return explicitValue;

        var fromEnv = System.Environment.GetEnvironmentVariable("NEX_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        return Load().Environment;
    }

    /// <summary>Resolve a persona: usa o valor fornecido explicitamente, depois env var, depois config.</summary>
    public static string? ResolvePersona(string? explicitValue)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
            return explicitValue;

        var fromEnv = System.Environment.GetEnvironmentVariable("NEX_PERSONA");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        return Load().Persona;
    }
}