namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider Ollama.
/// Valores vêm de appsettings.json seção "AiRuntime:Ollama".
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "AiRuntime:Ollama";

    /// <summary>URL base do Ollama (default: http://localhost:11434).</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Timeout em segundos para cada requisição (default: 120).</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Número máximo de retries em caso de falha (default: 2).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Modelo default para chat/completion (default: deepseek-r1:1.5b).</summary>
    public string DefaultChatModel { get; set; } = "deepseek-r1:1.5b";

    /// <summary>Indica se o provider está habilitado (default: true).</summary>
    public bool Enabled { get; set; } = true;
}
