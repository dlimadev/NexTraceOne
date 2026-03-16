namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Âmbito de uma delegação de administração.
/// Define quais permissões o delegado recebe sobre a equipa ou domínio.
/// </summary>
public enum DelegationScope
{
    /// <summary>Administração limitada ao contexto da equipa.</summary>
    TeamAdmin = 0,

    /// <summary>Administração limitada ao contexto do domínio.</summary>
    DomainAdmin = 1,

    /// <summary>Acesso apenas de leitura — sem permissão de alteração.</summary>
    ReadOnly = 2,

    /// <summary>Administração completa sobre o recurso delegado.</summary>
    FullAdmin = 3
}
