namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Âmbito de aplicação de uma configuração dentro da plataforma.
/// Define a que nível da hierarquia organizacional a configuração se aplica,
/// permitindo herança e sobreposição controlada de valores.
/// </summary>
public enum ConfigurationScope
{
    /// <summary>Configuração global de sistema — aplica-se a toda a plataforma sem exceção.</summary>
    System = 0,

    /// <summary>Configuração ao nível do tenant — sobrepõe valores de sistema para um tenant específico.</summary>
    Tenant = 1,

    /// <summary>Configuração ao nível do ambiente — aplica-se a um ambiente específico (ex: produção, staging).</summary>
    Environment = 2,

    /// <summary>Configuração ao nível do papel — aplica-se a utilizadores com um papel específico dentro da plataforma.</summary>
    Role = 3,

    /// <summary>Configuração ao nível da equipa — aplica-se a uma equipa específica e aos seus membros.</summary>
    Team = 4,

    /// <summary>Configuração ao nível do utilizador — valor personalizado para um utilizador individual.</summary>
    User = 5
}
