namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Catálogo estático de definições de ferramentas oficiais da plataforma.
/// Utilizado para seed da tabela aik_tool_definitions com as ferramentas
/// nativas do NexTraceOne disponíveis para agentes IA.
///
/// Cada ferramenta define capacidades, schema de parâmetros, nível de risco
/// e metadados de execução. O catálogo é determinístico e idempotente.
/// </summary>
public static class DefaultToolDefinitionCatalog
{
    /// <summary>Definição de uma ferramenta para seed.</summary>
    public sealed record ToolSeedDefinition(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string ParametersSchema,
        int RiskLevel,
        bool RequiresApproval,
        int TimeoutMs);

    /// <summary>
    /// Retorna a lista completa de ferramentas oficiais da plataforma.
    /// </summary>
    public static IReadOnlyList<ToolSeedDefinition> GetAll() => Tools;

    private static readonly IReadOnlyList<ToolSeedDefinition> Tools = new[]
    {
        new ToolSeedDefinition(
            Name: "get_service_health",
            DisplayName: "Get Service Health",
            Description: "Retrieves current health status, SLIs, and recent incidents for a specific service.",
            Category: "service_catalog",
            ParametersSchema: """{"type":"object","properties":{"serviceId":{"type":"string","description":"Service identifier"},"includeDependencies":{"type":"boolean","description":"Include dependency health"}},"required":["serviceId"]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 15000),

        new ToolSeedDefinition(
            Name: "list_services",
            DisplayName: "List Services Info",
            Description: "Lists services with optional filtering by team, domain, or status.",
            Category: "service_catalog",
            ParametersSchema: """{"type":"object","properties":{"teamId":{"type":"string","description":"Filter by team"},"status":{"type":"string","description":"Filter by status"},"limit":{"type":"integer","description":"Max results"}},"required":[]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 10000),

        new ToolSeedDefinition(
            Name: "list_recent_changes",
            DisplayName: "List Recent Changes",
            Description: "Lists recent changes and deployments for a service or across the platform.",
            Category: "change_governance",
            ParametersSchema: """{"type":"object","properties":{"serviceId":{"type":"string","description":"Filter by service"},"environment":{"type":"string","description":"Filter by environment"},"days":{"type":"integer","description":"Number of days to look back"}},"required":[]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 15000),

        new ToolSeedDefinition(
            Name: "get_contract_details",
            DisplayName: "Get Contract Details",
            Description: "Retrieves contract definition, version history, and consumers for an API or event contract.",
            Category: "contract_governance",
            ParametersSchema: """{"type":"object","properties":{"contractId":{"type":"string","description":"Contract identifier"},"includeVersionHistory":{"type":"boolean","description":"Include version history"}},"required":["contractId"]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 10000),

        new ToolSeedDefinition(
            Name: "search_incidents",
            DisplayName: "Search Incidents",
            Description: "Searches incidents by service, severity, status, or time range.",
            Category: "operations",
            ParametersSchema: """{"type":"object","properties":{"serviceId":{"type":"string","description":"Filter by service"},"severity":{"type":"string","description":"Filter by severity"},"status":{"type":"string","description":"Filter by status"},"days":{"type":"integer","description":"Number of days to look back"}},"required":[]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 15000),

        new ToolSeedDefinition(
            Name: "get_token_usage_summary",
            DisplayName: "Get Token Usage Summary",
            Description: "Retrieves AI token usage statistics for a user, team, or tenant.",
            Category: "ai_governance",
            ParametersSchema: """{"type":"object","properties":{"scope":{"type":"string","description":"Scope: user, team, tenant"},"scopeValue":{"type":"string","description":"Scope identifier"},"period":{"type":"string","description":"Period: day, week, month"}},"required":["scope","scopeValue"]}""",
            RiskLevel: 0,
            RequiresApproval: false,
            TimeoutMs: 10000),
    };
}
