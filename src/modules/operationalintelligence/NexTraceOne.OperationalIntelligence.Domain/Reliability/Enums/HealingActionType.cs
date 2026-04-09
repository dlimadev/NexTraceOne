namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Tipo de ação de auto-recuperação recomendada pelo motor de self-healing.
/// Cada valor representa uma classe de ação que pode ser executada para mitigar
/// a causa raiz de um incidente.
/// </summary>
public enum HealingActionType
{
    /// <summary>Reiniciar o serviço ou instância afectada.</summary>
    Restart = 0,

    /// <summary>Escalar horizontalmente o serviço para absorver carga.</summary>
    Scale = 1,

    /// <summary>Reverter a última mudança associada ao incidente.</summary>
    Rollback = 2,

    /// <summary>Alterar configuração do serviço para corrigir o problema.</summary>
    ConfigChange = 3,

    /// <summary>Activar ou desactivar circuit breaker para isolar falha.</summary>
    CircuitBreakerToggle = 4,

    /// <summary>Limpar cache do serviço para resolver inconsistências.</summary>
    CacheClear = 5,
}
