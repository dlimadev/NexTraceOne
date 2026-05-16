using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

/// <summary>
/// Classe base para agentes implementados como plugins Semantic Kernel.
/// Derive desta classe e anote métodos com [KernelFunction] + [Description]
/// para expor capacidades ao orquestrador SK.
/// </summary>
public abstract class SemanticKernelPluginBase : IAiAgentPlugin
{
    /// <inheritdoc />
    public abstract string PluginName { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <summary>
    /// Cria um plugin SK a partir desta instância usando reflection sobre [KernelFunction].
    /// </summary>
    public virtual KernelPlugin ToKernelPlugin()
    {
        return KernelPluginFactory.CreateFromObject(this, PluginName);
    }
}
