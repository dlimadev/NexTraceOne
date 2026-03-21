using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Context;

/// <summary>
/// Value Object que representa um achado de risco identificado pela IA
/// durante análise de promoção ou comparação de ambientes.
///
/// Um RiskFinding é um sinal concreto e rastreável — não uma recomendação vaga.
/// Deve indicar: o que foi encontrado, onde, quando, qual o impacto potencial
/// e qual a evidência que sustenta o achado.
///
/// A rastreabilidade é fundamental: o usuário deve poder chegar
/// da recomendação da IA até a evidência bruta (trace, log, contrato, incidente).
/// </summary>
public sealed class RiskFinding : ValueObject
{
    /// <summary>Identificador único do achado (para rastreabilidade).</summary>
    public Guid FindingId { get; }

    /// <summary>Categoria do risco.</summary>
    public RiskCategory Category { get; }

    /// <summary>Severidade do risco.</summary>
    public RiskSeverity Severity { get; }

    /// <summary>Título curto e descritivo do achado.</summary>
    public string Title { get; }

    /// <summary>Descrição detalhada do achado e impacto potencial.</summary>
    public string Description { get; }

    /// <summary>Serviço afetado (quando aplicável).</summary>
    public string? AffectedService { get; }

    /// <summary>
    /// Referências às evidências que sustentam o achado
    /// (ex.: trace ID, incident ID, contract diff hash, metric snapshot ID).
    /// </summary>
    public IReadOnlyList<string> EvidenceReferences { get; }

    /// <summary>Data/hora UTC em que o achado foi identificado.</summary>
    public DateTimeOffset DetectedAt { get; }

    /// <summary>
    /// Sugestão de ação para mitigar ou investigar o risco.
    /// </summary>
    public string? SuggestedAction { get; }

    private RiskFinding(
        Guid findingId,
        RiskCategory category,
        RiskSeverity severity,
        string title,
        string description,
        string? affectedService,
        IReadOnlyList<string> evidenceReferences,
        DateTimeOffset detectedAt,
        string? suggestedAction)
    {
        FindingId = findingId;
        Category = category;
        Severity = severity;
        Title = title;
        Description = description;
        AffectedService = affectedService;
        EvidenceReferences = evidenceReferences;
        DetectedAt = detectedAt;
        SuggestedAction = suggestedAction;
    }

    /// <summary>Cria um novo achado de risco.</summary>
    public static RiskFinding Create(
        RiskCategory category,
        RiskSeverity severity,
        string title,
        string description,
        DateTimeOffset detectedAt,
        string? affectedService = null,
        IEnumerable<string>? evidenceReferences = null,
        string? suggestedAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new RiskFinding(
            Guid.NewGuid(),
            category,
            severity,
            title,
            description,
            affectedService,
            (evidenceReferences ?? []).ToList().AsReadOnly(),
            detectedAt,
            suggestedAction);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FindingId;
    }
}

/// <summary>Categorias de risco identificáveis pela IA.</summary>
public enum RiskCategory
{
    /// <summary>Regressão de performance ou latência.</summary>
    PerformanceRegression = 1,

    /// <summary>Aumento na taxa de erros ou exceções.</summary>
    ErrorRateIncrease = 2,

    /// <summary>Incompatibilidade de contrato (breaking change).</summary>
    ContractBreakingChange = 3,

    /// <summary>Risco de dependência (serviço dependente instável).</summary>
    DependencyRisk = 4,

    /// <summary>Dado fora do padrão esperado (anomalia).</summary>
    DataAnomaly = 5,

    /// <summary>Falha de segurança ou exposição potencial.</summary>
    SecurityConcern = 6,

    /// <summary>Cobertura de testes insuficiente para área modificada.</summary>
    InsufficientTestCoverage = 7,

    /// <summary>Configuração faltante ou incorreta para o ambiente de destino.</summary>
    ConfigurationGap = 8,

    /// <summary>Incidente recente não resolvido que pode impactar a promoção.</summary>
    UnresolvedIncident = 9
}

/// <summary>Severidade de um achado de risco.</summary>
public enum RiskSeverity
{
    /// <summary>Informativo — não bloqueia, apenas registra.</summary>
    Info = 1,

    /// <summary>Aviso — revisar antes de prosseguir.</summary>
    Warning = 2,

    /// <summary>Alto — recomenda-se resolver antes da promoção.</summary>
    High = 3,

    /// <summary>Crítico — bloqueia recomendação de promoção.</summary>
    Critical = 4
}
