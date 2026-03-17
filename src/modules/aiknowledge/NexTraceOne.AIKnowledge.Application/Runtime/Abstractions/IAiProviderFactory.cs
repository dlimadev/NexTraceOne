namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Fábrica para resolução de providers de IA registrados.
/// Centraliza a resolução sem espalhar lógica de provider pelo sistema.
/// </summary>
public interface IAiProviderFactory
{
    /// <summary>Obtém um provider por ID.</summary>
    IAiProvider? GetProvider(string providerId);

    /// <summary>Obtém um provider de chat/completion por ID.</summary>
    IChatCompletionProvider? GetChatProvider(string providerId);

    /// <summary>Lista todos os providers registrados.</summary>
    IReadOnlyList<IAiProvider> GetAllProviders();
}
