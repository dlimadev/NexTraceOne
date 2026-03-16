using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetModuleAdoption;

/// <summary>
/// Retorna métricas de adoção por módulo do produto.
/// Responde: quais módulos são mais usados? Quais têm baixa adoção?
/// Quais capabilities têm uso real versus superficial?
/// </summary>
public static class GetModuleAdoption
{
    /// <summary>Query para métricas de adoção de módulos.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna adoção por módulo.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var modules = new List<ModuleAdoptionDto>
            {
                new(ProductModule.SourceOfTruth, "Source of Truth", 89, 3_240, 156, 92.3m, TrendDirection.Improving,
                    new[] { "query_contracts", "view_services", "search_schemas" }),
                new(ProductModule.ContractStudio, "Contract Studio", 76, 2_810, 134, 78.4m, TrendDirection.Improving,
                    new[] { "create_draft", "edit_contract", "publish", "validate" }),
                new(ProductModule.ChangeIntelligence, "Change Intelligence", 68, 2_150, 98, 71.2m, TrendDirection.Stable,
                    new[] { "view_changes", "blast_radius", "correlation" }),
                new(ProductModule.AiAssistant, "AI Assistant", 72, 1_980, 112, 65.8m, TrendDirection.Improving,
                    new[] { "prompt_submit", "response_used", "context_query" }),
                new(ProductModule.Incidents, "Incidents", 54, 1_420, 87, 58.1m, TrendDirection.Stable,
                    new[] { "investigate", "mitigation_start", "mitigation_complete" }),
                new(ProductModule.Reliability, "Reliability", 48, 1_247, 62, 45.6m, TrendDirection.Declining,
                    new[] { "view_dashboard", "set_objectives", "review_sla" }),
                new(ProductModule.Governance, "Governance", 41, 980, 45, 52.3m, TrendDirection.Stable,
                    new[] { "view_policies", "compliance_check", "evidence_export" }),
                new(ProductModule.ExecutiveViews, "Executive Views", 35, 720, 28, 88.2m, TrendDirection.Improving,
                    new[] { "overview", "risk_heatmap", "maturity", "benchmarking" }),
                new(ProductModule.FinOps, "FinOps", 32, 650, 24, 42.1m, TrendDirection.Stable,
                    new[] { "cost_view", "waste_analysis", "efficiency" }),
                new(ProductModule.Runbooks, "Runbooks", 38, 540, 34, 35.7m, TrendDirection.Declining,
                    new[] { "view_runbook", "execute_step" }),
                new(ProductModule.IntegrationHub, "Integration Hub", 22, 380, 18, 28.4m, TrendDirection.Stable,
                    new[] { "configure_connector", "view_execution", "freshness" }),
                new(ProductModule.Automation, "Automation", 28, 420, 22, 31.5m, TrendDirection.Improving,
                    new[] { "create_workflow", "execute", "schedule" }),
                new(ProductModule.Search, "Search", 94, 4_120, 210, 96.1m, TrendDirection.Stable,
                    new[] { "global_search", "command_palette", "filter" }),
                new(ProductModule.DeveloperPortal, "Developer Portal", 18, 280, 14, 22.3m, TrendDirection.Stable,
                    new[] { "browse_apis", "playground", "subscribe" })
            };

            var response = new Response(
                Modules: modules,
                OverallAdoptionScore: 74.5m,
                MostAdopted: ProductModule.Search,
                LeastAdopted: ProductModule.DeveloperPortal,
                BiggestGrowth: ProductModule.ContractStudio,
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com adoção por módulo.</summary>
    public sealed record Response(
        IReadOnlyList<ModuleAdoptionDto> Modules,
        decimal OverallAdoptionScore,
        ProductModule MostAdopted,
        ProductModule LeastAdopted,
        ProductModule BiggestGrowth,
        string PeriodLabel);

    /// <summary>Métricas de adoção de um módulo individual.</summary>
    public sealed record ModuleAdoptionDto(
        ProductModule Module,
        string ModuleName,
        int AdoptionPercent,
        long TotalActions,
        int UniqueUsers,
        decimal DepthScore,
        TrendDirection Trend,
        IReadOnlyList<string> TopFeatures);
}
