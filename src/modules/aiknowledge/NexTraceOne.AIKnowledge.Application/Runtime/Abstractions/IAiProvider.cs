namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Abstração base para provedores de IA. Define o contrato mínimo 
/// para qualquer provider de inferência (Ollama, OpenAI, Azure, etc.).
/// Domínio não depende de SDK externo — apenas de contratos.
/// </summary>
public interface IAiProvider
{
    /// <summary>Identificador único do provider (ex: "ollama", "openai").</summary>
    string ProviderId { get; }

    /// <summary>Nome de exibição do provider.</summary>
    string DisplayName { get; }

    /// <summary>Indica se o provider é local (true) ou externo/cloud (false).</summary>
    bool IsLocal { get; }

    /// <summary>Verifica se o provider está disponível e saudável.</summary>
    Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista os modelos disponíveis neste provider.</summary>
    Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Resultado do health check de um provider.</summary>
public sealed record AiProviderHealthResult(
    bool IsHealthy,
    string ProviderId,
    string? Message = null,
    TimeSpan? ResponseTime = null);

/// <summary>Informação sobre um modelo disponível num provider.</summary>
public sealed record AiProviderModelInfo(
    string ModelId,
    string DisplayName,
    long? ParameterSize = null,
    IReadOnlyList<string>? Capabilities = null);
