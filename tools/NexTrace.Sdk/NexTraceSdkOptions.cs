namespace NexTrace.Sdk;

/// <summary>
/// Opções de configuração do NexTrace.Sdk.
/// Fornece base URL, token de autenticação e comportamento de rede.
/// </summary>
public sealed class NexTraceSdkOptions
{
    /// <summary>URL base da API NexTraceOne (ex: https://nextraceone.example.com).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Token de autenticação Bearer para a API.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Timeout em segundos para cada pedido HTTP. Padrão: 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Número de tentativas automáticas em falha transitória. Padrão: 2.</summary>
    public int RetryCount { get; set; } = 2;
}
