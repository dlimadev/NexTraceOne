using Microsoft.AspNetCore.Builder;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API do módulo ChangeIntelligence.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Delega para ficheiros especializados por domínio funcional:
/// - <see cref="DeploymentEndpoints"/> — notificações de deployment, estado e rollback.
/// - <see cref="ReleaseQueryEndpoints"/> — consulta de releases e histórico.
/// - <see cref="AnalysisEndpoints"/> — classificação, blast radius, score e work items.
/// - <see cref="IntelligenceEndpoints"/> — marcadores externos, sumário, baseline, review e rollback assessment.
/// - <see cref="FreezeEndpoints"/> — gestão de janelas de freeze.
/// - <see cref="ChangeConfidenceEndpoints"/> — catálogo de mudanças, resumo e Change Confidence.
///
/// Este padrão segue a mesma abordagem do módulo Identity, reduzindo o tamanho
/// de cada ficheiro individual e melhorando a navegabilidade do código.
/// </summary>
public sealed class ChangeIntelligenceEndpointModule
{
    /// <summary>
    /// Ponto de entrada do assembly scanning — regista todos os sub-módulos de endpoints.
    /// </summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/releases");

        DeploymentEndpoints.Map(group);
        ReleaseQueryEndpoints.Map(group);
        AnalysisEndpoints.Map(group);
        IntelligenceEndpoints.Map(group);

        FreezeEndpoints.Map(app);
        ChangeConfidenceEndpoints.Map(app);
    }
}
