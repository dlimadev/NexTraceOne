using Microsoft.AspNetCore.Builder;

namespace NexTraceOne.Workflow.API.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API do módulo Workflow.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Delega para ficheiros especializados por domínio funcional:
/// - <see cref="TemplateEndpoints"/> — gestão de templates de workflow.
/// - <see cref="ApprovalEndpoints"/> — criação de instâncias, aprovações e decisões.
/// - <see cref="StatusEndpoints"/> — consulta de status, pendências e escalação de SLA.
/// - <see cref="EvidencePackEndpoints"/> — geração e consulta de evidence packs.
///
/// Este padrão segue a mesma abordagem do módulo Identity, reduzindo o tamanho
/// de cada ficheiro e agrupando endpoints por responsabilidade funcional (SRP).
/// </summary>
public sealed class WorkflowEndpointModule
{
    /// <summary>
    /// Ponto de entrada do assembly scanning — regista todos os sub-módulos de endpoints.
    /// </summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workflow");

        TemplateEndpoints.Map(group);
        ApprovalEndpoints.Map(group);
        StatusEndpoints.Map(group);
        EvidencePackEndpoints.Map(group);
    }
}
