namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider Google Gemini (externo, cloud).
/// Valores vêm de appsettings.json seção "AiRuntime:Gemini".
/// A chave de API deve ser fornecida via variável de ambiente ou secrets manager em produção.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "AiRuntime:Gemini";

    /// <summary>URL base da API Gemini (default: https://generativelanguage.googleapis.com/v1beta).</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Chave de API Google Gemini. Obrigatória para activar o provider.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Modelo de chat padrão (default: gemini-1.5-pro).</summary>
    public string DefaultChatModel { get; set; } = "gemini-1.5-pro";

    /// <summary>Timeout em segundos para cada requisição (default: 60).</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Temperatura padrão para geração (default: 0.3).</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    /// <summary>Número máximo de tokens de saída por defecto (default: 4096).</summary>
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>Indica se o provider está habilitado (default: false).</summary>
    public bool Enabled { get; set; }

    /// <summary>Indica se a chave de API está configurada e o provider pode ser activado.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
