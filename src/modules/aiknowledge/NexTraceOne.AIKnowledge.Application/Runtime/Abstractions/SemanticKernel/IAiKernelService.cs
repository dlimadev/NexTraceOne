using Microsoft.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

/// <summary>
/// Serviço de orquestração via Microsoft Semantic Kernel.
/// Abstrai a criação e gestão do Kernel SK, plugins e funções.
/// </summary>
public interface IAiKernelService
{
    /// <summary>
    /// Cria um Kernel SK configurado para um provider e modelo específicos.
    /// </summary>
    Kernel CreateKernel(string providerId, string modelId);

    /// <summary>
    /// Regista um plugin no kernel.
    /// </summary>
    void RegisterPlugin(Kernel kernel, KernelPlugin plugin);

    /// <summary>
    /// Executa uma função de um plugin de forma assíncrona.
    /// </summary>
    Task<FunctionResult> ExecutePluginAsync(
        Kernel kernel,
        string pluginName,
        string functionName,
        KernelArguments arguments,
        CancellationToken ct = default);

    /// <summary>
    /// Executa chat completion via Kernel com histórico e plugins ativos.
    /// </summary>
    Task<string> ExecuteChatAsync(
        Kernel kernel,
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default);
}
