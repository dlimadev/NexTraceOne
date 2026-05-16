using Microsoft.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

/// <summary>
/// Interface para agentes implementados como plugins Semantic Kernel.
/// Cada agente expõe funções nativas (native functions) que o SK pode orquestrar.
/// </summary>
public interface IAiAgentPlugin
{
    /// <summary>Nome único do plugin/agente.</summary>
    string PluginName { get; }

    /// <summary>Descrição do plugin para o modelo de IA.</summary>
    string Description { get; }

    /// <summary>Converte este agente num plugin SK registrável.</summary>
    KernelPlugin ToKernelPlugin();
}
