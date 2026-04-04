namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider OpenAI (externo, cloud).
/// Valores vêm de appsettings.json seção "AiRuntime:OpenAI".
/// A chave de API deve ser fornecida via variável de ambiente ou secrets manager em produção.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "AiRuntime:OpenAI";

    /// <summary>URL base da API OpenAI (default: https://api.openai.com/v1).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Chave de API OpenAI. Obrigatória para activar o provider.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Modelo de chat padrão (default: gpt-4o-mini).</summary>
    public string DefaultChatModel { get; set; } = "gpt-4o-mini";

    /// <summary>Timeout em segundos para cada requisição (default: 60).</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Temperatura padrão para geração (default: 0.3).</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    /// <summary>Número máximo de tokens de saída por defecto (default: 2048).</summary>
    public int DefaultMaxTokens { get; set; } = 2048;

    /// <summary>Indica se o provider está habilitado (default: false — activo só quando ApiKey está configurado).</summary>
    public bool Enabled { get; set; }

    /// <summary>Modelo de embeddings padrão (default: text-embedding-3-small).</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Indica se a chave de API está configurada e o provider pode ser activado.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
