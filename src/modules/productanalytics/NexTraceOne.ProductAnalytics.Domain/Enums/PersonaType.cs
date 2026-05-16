namespace NexTraceOne.ProductAnalytics.Domain.Enums;

/// <summary>
/// Tipos de persona conhecidos na plataforma NexTraceOne.
/// Usado para calcular taxas de adoção e segmentação por perfil funcional.
/// </summary>
public enum PersonaType
{
    Engineer = 0,
    TechLead = 1,
    Architect = 2,
    Product = 3,
    Executive = 4,
    PlatformAdmin = 5,
    Auditor = 6
}
