namespace NexTraceOne.ProductAnalytics.Domain.Enums;

/// <summary>
/// Tipos de sinais de fricção no produto.
/// Cada tipo indica um ponto onde o utilizador pode estar a encontrar dificuldade
/// ou onde a experiência não está a entregar valor esperado.
/// </summary>
public enum FrictionSignalType
{
    /// <summary>Busca executada sem nenhum resultado.</summary>
    ZeroResultSearch = 0,

    /// <summary>Empty state encontrado repetidamente no mesmo módulo.</summary>
    RepeatedEmptyState = 1,

    /// <summary>Jornada iniciada mas abandonada antes da conclusão.</summary>
    AbortedJourney = 2,

    /// <summary>Tentativa repetida da mesma ação — indica dificuldade ou bug.</summary>
    RepeatedRetry = 3,

    /// <summary>Módulo acessado mas abandonado rapidamente.</summary>
    ModuleAbandonment = 4,

    /// <summary>Ação bloqueada por política — pode indicar frustração se frequente.</summary>
    BlockedByPolicy = 5,

    /// <summary>Quota excedida — indica limitação que impede uso do produto.</summary>
    QuotaExceeded = 6,

    /// <summary>Loop de navegação — utilizador volta ao mesmo ponto sem progredir.</summary>
    NavigationLoop = 7,

    /// <summary>Ação importante descoberta tardiamente — indica problema de discoverability.</summary>
    LateDiscovery = 8
}
