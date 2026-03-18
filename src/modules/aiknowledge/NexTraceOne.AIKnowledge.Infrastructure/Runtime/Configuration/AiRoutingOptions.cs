namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

/// <summary>
/// Opcoes de roteamento para inferencia de IA.
/// Define provider preferencial e politica de fallback explicito.
/// </summary>
public sealed class AiRoutingOptions
{
    public const string SectionName = "AiRuntime:Routing";

    /// <summary>Provider preferencial quando nao informado pela camada de aplicacao.</summary>
    public string? PreferredProvider { get; set; }

    /// <summary>Modelo preferencial para chat quando houver override de provider.</summary>
    public string? PreferredChatModel { get; set; }

    /// <summary>
    /// Quando true, retorna fallback deterministico explicito caso o provider esteja indisponivel.
    /// Quando false, propaga erro para o handler retornar falha ao cliente.
    /// </summary>
    public bool EnableDeterministicFallback { get; set; } = true;

    /// <summary>Prefixo textual do fallback explicito para facilitar rastreabilidade.</summary>
    public string FallbackPrefix { get; set; } = "[FALLBACK_PROVIDER_UNAVAILABLE]";
}
