namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Tipos de marcadores externos recebidos de ferramentas CI/CD.
/// Cada marcador representa um evento significativo no ciclo de vida
/// de build/deploy reportado por GitHub, GitLab, Jenkins, Azure DevOps, etc.
/// </summary>
public enum MarkerType
{
    /// <summary>Build concluído com sucesso na pipeline CI/CD.</summary>
    BuildCompleted = 0,
    /// <summary>Artefato versionado e publicado no registry.</summary>
    ArtifactVersioned = 1,
    /// <summary>Deploy iniciado no ambiente alvo.</summary>
    DeploymentStarted = 2,
    /// <summary>Deploy concluído com sucesso.</summary>
    DeploymentFinished = 3,
    /// <summary>Rollback iniciado no ambiente.</summary>
    RollbackStarted = 4,
    /// <summary>Rollback concluído.</summary>
    RollbackFinished = 5,
    /// <summary>Canary release iniciado.</summary>
    CanaryStarted = 6,
    /// <summary>Migração de dados iniciada.</summary>
    MigrationStarted = 7,
    /// <summary>Migração de dados concluída.</summary>
    MigrationCompleted = 8,
    /// <summary>Marcador genérico de publicação.</summary>
    PublicationMarker = 9,
    /// <summary>Testes automatizados concluídos.</summary>
    TestsCompleted = 10,
    /// <summary>Aprovação de segurança obtida.</summary>
    SecurityApproval = 11
}
