namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Configuração do provider GitHub Copilot / GitHub Models (externo, cloud).
/// Valores vêm de appsettings.json seção "AiRuntime:GitHubCopilot".
/// O token deve ser um Personal Access Token (PAT) ou GitHub App token com acesso a GitHub Models.
/// </summary>
public sealed class GitHubCopilotOptions
{
    public const string SectionName = "AiRuntime:GitHubCopilot";

    /// <summary>URL base da API GitHub Models (default: https://models.inference.ai.azure.com).</summary>
    public string BaseUrl { get; set; } = "https://models.inference.ai.azure.com";

    /// <summary>Token de autenticação GitHub (PAT ou GitHub App token). Obrigatório para activar o provider.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Modelo de chat padrão (default: gpt-4o).</summary>
    public string DefaultChatModel { get; set; } = "gpt-4o";

    /// <summary>Timeout em segundos para cada requisição (default: 60).</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Temperatura padrão para geração (default: 0.3).</summary>
    public double DefaultTemperature { get; set; } = 0.3;

    /// <summary>Número máximo de tokens de saída por defecto (default: 4096).</summary>
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>Indica se o provider está habilitado (default: false).</summary>
    public bool Enabled { get; set; }

    /// <summary>Indica se o token de API está configurado e o provider pode ser activado.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiToken);
}
