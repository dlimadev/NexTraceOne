using System.Diagnostics.CodeAnalysis;

namespace NexTraceOne.Ingestion.Api.Security;

/// <summary>
/// Configuração de segredos HMAC-SHA256 para validação de assinatura de webhooks recebidos.
/// Cada entrada mapeia um nome de fonte (ex.: "SonarQube", "Commits") ao respectivo segredo.
/// Configurar via variável de ambiente: Security__WebhookSecrets__SonarQube=&lt;secret&gt;
/// </summary>
public sealed class WebhookSignatureOptions
{
    /// <summary>Nome da secção de configuração no appsettings.</summary>
    public const string SectionName = "Security:WebhookSecrets";

    /// <summary>
    /// Dicionário de segredos indexado pelo nome da fonte de webhook.
    /// Chave: nome da fonte (ex.: "SonarQube", "Commits").
    /// Valor: segredo HMAC-SHA256 em texto plano (nunca em Base64; usado directamente como chave HMAC).
    /// </summary>
    public Dictionary<string, string> Secrets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tenta obter o segredo configurado para a fonte indicada.
    /// Retorna false se a fonte não estiver configurada ou o segredo estiver vazio.
    /// </summary>
    public bool TryGetSecret(string sourceName, [NotNullWhen(true)] out string? secret)
    {
        if (Secrets.TryGetValue(sourceName, out secret) && !string.IsNullOrWhiteSpace(secret))
            return true;

        secret = null;
        return false;
    }
}
