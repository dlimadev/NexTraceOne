namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider Anthropic/Claude (externo, cloud).
/// Valores vêm de appsettings.json seção "AiRuntime:Anthropic".
/// A chave de API deve ser fornecida via variável de ambiente ou secrets manager em produção.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "AiRuntime:Anthropic";

    /// <summary>URL base da API Anthropic (default: https://api.anthropic.com).</summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>Chave de API Anthropic. Obrigatória para activar o provider.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Versão da API Anthropic (default: 2023-06-01).</summary>
    public string ApiVersion { get; set; } = "2023-06-01";

    /// <summary>Modelo de chat padrão (default: claude-3-5-haiku-20241022).</summary>
    public string DefaultChatModel { get; set; } = "claude-3-5-haiku-20241022";

    /// <summary>Timeout em segundos para cada requisição (default: 60).</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Temperatura padrão para geração (default: 0.3).</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    /// <summary>Número máximo de tokens de saída por defecto (default: 2048).</summary>
    public int DefaultMaxTokens { get; set; } = 2048;

    /// <summary>Indica se o provider está habilitado (default: false — activo só quando ApiKey está configurado).</summary>
    public bool Enabled { get; set; }

    /// <summary>Indica se a chave de API está configurada e o provider pode ser activado.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
