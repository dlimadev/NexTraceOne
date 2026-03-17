namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>Status do deployment de uma release.</summary>
public enum DeploymentStatus
{
    /// <summary>Aguardando execução.</summary>
    Pending = 0,
    /// <summary>Em execução.</summary>
    Running = 1,
    /// <summary>Concluído com sucesso.</summary>
    Succeeded = 2,
    /// <summary>Falhou.</summary>
    Failed = 3,
    /// <summary>Revertido (rollback).</summary>
    RolledBack = 4
}
