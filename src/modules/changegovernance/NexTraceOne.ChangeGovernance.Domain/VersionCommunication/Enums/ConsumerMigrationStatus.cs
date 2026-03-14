namespace NexTraceOne.VersionCommunication.Domain.Enums;

/// <summary>
/// Status do processo de migração de um consumidor específico para uma nova versão de API.
/// Rastreia o progresso individual de cada consumidor no plano de rollout.
/// </summary>
public enum ConsumerMigrationStatus
{
    /// <summary>Pendente — consumidor identificado mas ainda não notificado.</summary>
    Pending = 0,

    /// <summary>Notificado — consumidor recebeu a notificação de migração.</summary>
    Notified = 1,

    /// <summary>Reconhecido — consumidor confirmou o recebimento e ciência da migração.</summary>
    Acknowledged = 2,

    /// <summary>Em progresso — consumidor iniciou o processo de migração.</summary>
    InProgress = 3,

    /// <summary>Concluído — consumidor completou a migração para a nova versão.</summary>
    Completed = 4,

    /// <summary>Ignorado — consumidor foi excluído do plano de migração (ex: descontinuado).</summary>
    Skipped = 5
}
