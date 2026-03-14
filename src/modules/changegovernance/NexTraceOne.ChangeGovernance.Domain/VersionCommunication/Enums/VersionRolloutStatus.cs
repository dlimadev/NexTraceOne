namespace NexTraceOne.VersionCommunication.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de um plano de rollout de versão.
/// Controla a progressão desde a criação do plano até a conclusão ou cancelamento.
/// </summary>
public enum VersionRolloutStatus
{
    /// <summary>Rascunho — plano criado mas ainda não anunciado aos consumidores.</summary>
    Draft = 0,

    /// <summary>Anunciado — consumidores foram notificados sobre a nova versão disponível.</summary>
    Announced = 1,

    /// <summary>Em progresso — migração ativa dos consumidores para a nova versão.</summary>
    InProgress = 2,

    /// <summary>Concluído — todos os consumidores foram migrados ou o prazo expirou.</summary>
    Completed = 3,

    /// <summary>Cancelado — plano de rollout foi cancelado antes da conclusão.</summary>
    Cancelled = 4
}
