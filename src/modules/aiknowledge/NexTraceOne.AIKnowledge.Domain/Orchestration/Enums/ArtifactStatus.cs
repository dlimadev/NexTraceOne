namespace NexTraceOne.AiOrchestration.Domain.Enums;

/// <summary>
/// Estado de revisão de um artefato de teste gerado por IA.
/// Fluxo: Draft → Reviewed → (Accepted | Rejected).
/// </summary>
public enum ArtifactStatus
{
    /// <summary>Artefato recém-gerado, aguardando revisão humana.</summary>
    Draft = 0,

    /// <summary>Artefato em processo de revisão.</summary>
    Reviewed = 1,

    /// <summary>Artefato aceito — pronto para uso em pipelines de teste.</summary>
    Accepted = 2,

    /// <summary>Artefato rejeitado — não atende aos requisitos de qualidade.</summary>
    Rejected = 3
}
