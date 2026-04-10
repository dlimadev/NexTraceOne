namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Ação a executar quando uma violação de compliance é detetada.
/// </summary>
public enum ComplianceAction
{
    /// <summary>Ignorar a violação.</summary>
    Ignore = 0,

    /// <summary>Gerar aviso sem bloquear.</summary>
    Warn = 1,

    /// <summary>Bloquear o build.</summary>
    BlockBuild = 2,

    /// <summary>Bloquear o deploy.</summary>
    BlockDeploy = 3
}
