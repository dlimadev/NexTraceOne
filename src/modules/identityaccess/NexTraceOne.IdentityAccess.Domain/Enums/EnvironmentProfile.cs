namespace NexTraceOne.IdentityAccess.Domain.Enums;

/// <summary>
/// Perfil operacional de um ambiente.
/// Define a natureza e o comportamento-base do ambiente, independente do nome dado pelo tenant.
/// Um ambiente chamado "QA-EUROPA" pode ter perfil Validation, enquanto "PROD-BR" tem perfil Production.
/// O perfil determina políticas padrão, nível de restrição, comportamento da IA e alertas operacionais.
/// </summary>
public enum EnvironmentProfile
{
    /// <summary>Ambiente de desenvolvimento ativo. Menor restrição, maior permissividade para testes rápidos.</summary>
    Development = 1,

    /// <summary>Ambiente de validação funcional e integração. Testes automatizados e QA.</summary>
    Validation = 2,

    /// <summary>Ambiente de homologação/staging. Comportamento próximo de produção, com dados sintéticos ou anonimizados.</summary>
    Staging = 3,

    /// <summary>Ambiente de produção. Máxima restrição, auditoria completa, acesso controlado.</summary>
    Production = 4,

    /// <summary>Sandbox isolado para experimentação sem impacto em outros ambientes.</summary>
    Sandbox = 5,

    /// <summary>Ambiente de recuperação de desastre. Replica produção em standby ou failover.</summary>
    DisasterRecovery = 6,

    /// <summary>Ambiente de treinamento e demonstração. Dados fictícios, sem acesso a sistemas reais.</summary>
    Training = 7,

    /// <summary>Ambiente de teste de aceite do usuário (UAT). Validação pelo negócio antes do go-live.</summary>
    UserAcceptanceTesting = 8,

    /// <summary>Ambiente de performance/carga. Focado em testes de stress e capacidade.</summary>
    PerformanceTesting = 9
}
