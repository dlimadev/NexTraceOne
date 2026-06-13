namespace NexTraceOne.Catalog.Contracts.Quality.ServiceInterfaces;

/// <summary>
/// Interface de comunicação entre módulos para avaliação do quality gate de um serviço
/// contra os gates declarados no template que o originou (TemplateManifestV2.QualityGates).
///
/// Usada pelo módulo ChangeGovernance para incorporar a qualidade de código como gate de
/// promoção, sem aceder diretamente ao DbContext do Catalog.
///
/// A avaliação é determinística (não depende de IA): compara cobertura e o quality gate do
/// SonarQube com o mínimo do template.
/// </summary>
public interface ICatalogQualityGateModule
{
    /// <summary>
    /// Avalia o quality gate do template para o serviço indicado.
    /// O template é resolvido por <paramref name="templateId"/>/<paramref name="templateSlug"/>
    /// quando fornecido; caso contrário a partir do template de origem do serviço.
    /// </summary>
    Task<TemplateQualityGateResult> EvaluateAsync(
        string serviceId,
        string tenantId,
        Guid? templateId = null,
        string? templateSlug = null,
        CancellationToken ct = default);
}

/// <summary>
/// Resultado da avaliação do quality gate de template, exposto a outros módulos.
/// </summary>
/// <param name="ServiceId">Identificador do serviço avaliado.</param>
/// <param name="Status">Estado da avaliação: Passed, Failed, NoQualityData, NoGatesDefined, NoTemplateLinked ou Error.</param>
/// <param name="Passed">Indica se o gate foi aprovado.</param>
/// <param name="RequiredCoverage">Cobertura mínima exigida pelo template (%).</param>
/// <param name="ActualCoverage">Cobertura real do serviço (%), quando disponível.</param>
/// <param name="SonarQualityGateStatus">Estado do quality gate do SonarQube, quando disponível.</param>
/// <param name="Breaches">Descrições legíveis das violações encontradas.</param>
public sealed record TemplateQualityGateResult(
    string ServiceId,
    string Status,
    bool Passed,
    int RequiredCoverage,
    double? ActualCoverage,
    string? SonarQualityGateStatus,
    IReadOnlyList<string> Breaches);
