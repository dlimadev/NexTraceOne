using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value object que encapsula a avaliação completa de compatibilidade entre duas versões
/// de contrato. Agrega o resultado do diff semântico com análise de impacto em consumers,
/// recomendação de versão, nível de risco e readiness para workflow de aprovação.
/// Gerado pelo pipeline: Parse → Normalize → Compare → Classify → Assess.
/// </summary>
public sealed record CompatibilityAssessment(
    /// <summary>Nível de mudança geral (Breaking, Additive, NonBreaking).</summary>
    ChangeLevel ChangeLevel,
    /// <summary>Indica se a mudança é retrocompatível com consumers existentes.</summary>
    bool IsBackwardCompatible,
    /// <summary>Versão semântica recomendada com base na análise.</summary>
    string RecommendedVersion,
    /// <summary>Nível de risco técnico normalizado de 0.0 (nenhum) a 1.0 (crítico).</summary>
    decimal RiskScore,
    /// <summary>Número de breaking changes detectadas.</summary>
    int BreakingChangeCount,
    /// <summary>Número de mudanças aditivas detectadas.</summary>
    int AdditiveChangeCount,
    /// <summary>Número de mudanças non-breaking detectadas.</summary>
    int NonBreakingChangeCount,
    /// <summary>Requer aprovação de workflow com base na criticidade da mudança.</summary>
    bool RequiresWorkflowApproval,
    /// <summary>Requer comunicação formal de mudança para consumers impactados.</summary>
    bool RequiresChangeNotification,
    /// <summary>Resumo legível da avaliação para uso em workflow e relatórios.</summary>
    string Summary,
    /// <summary>Protocolo do contrato avaliado.</summary>
    ContractProtocol Protocol);
