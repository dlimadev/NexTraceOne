namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider LM Studio — local, OpenAI-compatible.
/// Valores vêm de appsettings.json seção "AiRuntime:LmStudio".
/// LM Studio expõe uma API compatível com OpenAI em http://localhost:1234/v1 por padrão.
/// </summary>
public sealed class LmStudioOptions
{
    public const string SectionName = "AiRuntime:LmStudio";

    /// <summary>URL base do LM Studio (default: http://localhost:1234/v1).</summary>
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";

    /// <summary>Timeout em segundos para cada requisição (default: 120).</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Número máximo de retries em caso de falha transitória (default: 1).</summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    /// Modelo padrão para chat/completion.
    /// Corresponde ao model identifier activo no LM Studio Server.
    /// Deixar em branco para usar o modelo carregado actualmente pelo servidor.
    /// </summary>
    public string DefaultChatModel { get; set; } = string.Empty;

    /// <summary>Temperatura padrão para geração (default: 0.3).</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    /// <summary>Número máximo de tokens de saída por defecto (default: 2048).</summary>
    public int DefaultMaxTokens { get; set; } = 2048;

    /// <summary>Indica se o provider está habilitado (default: false).</summary>
    public bool Enabled { get; set; }
}
