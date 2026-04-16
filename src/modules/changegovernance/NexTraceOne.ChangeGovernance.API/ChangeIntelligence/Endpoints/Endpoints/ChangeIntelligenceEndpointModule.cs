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
/// - <see cref="TraceCorrelationEndpoints"/> — correlação trace → release (P5.2).
/// - <see cref="FreezeEndpoints"/> — gestão de janelas de freeze.
/// - <see cref="ChangeConfidenceEndpoints"/> — catálogo de mudanças, resumo e Change Confidence.
/// - <see cref="CommitPoolEndpoints"/> — commit pool, work items por release.
/// - <see cref="ApprovalGatewayEndpoints"/> — approval gateway externo (outbound + callback).
/// - <see cref="ReleaseIngestEndpoints"/> — ingestão de release externa + relatório de impacto.
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
        TraceCorrelationEndpoints.Map(group);
        ApprovalGatewayEndpoints.Map(group);
        ReleaseIngestEndpoints.Map(group);

        FreezeEndpoints.Map(app);
        ChangeConfidenceEndpoints.Map(app);

        // Commit pool precisa de acesso ao group e ao app raiz (para /api/v1/integrations/commits)
        CommitPoolEndpoints.Map(group, app);
    }
}
