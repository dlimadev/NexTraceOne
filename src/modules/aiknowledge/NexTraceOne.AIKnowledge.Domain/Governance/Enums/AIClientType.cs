namespace NexTraceOne.AiGovernance.Domain.Enums;

/// <summary>
/// Tipo de cliente que originou a interação com IA.
/// Utilizado para auditoria e segmentação de políticas por canal de acesso.
/// </summary>
public enum AIClientType
{
    /// <summary>Acesso pela interface web do NexTraceOne.</summary>
    Web,

    /// <summary>Acesso pela extensão VS Code.</summary>
    VsCode,

    /// <summary>Acesso pela extensão Visual Studio.</summary>
    VisualStudio,

    /// <summary>Acesso direto pela API programática.</summary>
    Api,

    /// <summary>Acesso originado por processos internos do sistema.</summary>
    System
}
