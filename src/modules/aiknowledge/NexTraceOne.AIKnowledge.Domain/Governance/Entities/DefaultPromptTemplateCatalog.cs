namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Catálogo estático de templates de prompt oficiais da plataforma.
/// Utilizado pela feature SeedDefaultPromptTemplates para garantir que o
/// sistema contenha os templates mínimos necessários para operação assistida por IA.
///
/// Cada template define conteúdo com placeholders, categoria funcional,
/// personas-alvo e metadados de configuração recomendada.
///
/// O catálogo é determinístico e idempotente — não cria templates duplicados.
/// </summary>
public static class DefaultPromptTemplateCatalog
{
    /// <summary>Definição de um template de prompt para seed.</summary>
    public sealed record TemplateDefinition(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string Content,
        string Variables,
        string TargetPersonas,
        string? ScopeHint,
        string Relevance);

    /// <summary>
    /// Retorna a lista completa de templates oficiais da plataforma.
    /// </summary>
    public static IReadOnlyList<TemplateDefinition> GetAll() => Templates;

    private static readonly IReadOnlyList<TemplateDefinition> Templates = new[]
    {
        // ── Operations ──────────────────────────────────────────────────
        new TemplateDefinition(
            Name: "incident-root-cause-analysis",
            DisplayName: "Incident Root Cause Analysis",
            Description: "Analyzes an incident to identify probable root causes based on service topology, recent changes, and telemetry.",
            Category: "analysis",
            Content: "Analyze the incident for service {{serviceName}} in environment {{environment}}. Consider recent changes in the last {{timeRange}}. Focus on: 1) Service dependencies and topology impact, 2) Recent deployments and configuration changes, 3) Telemetry anomalies and error patterns. Provide a structured root cause analysis with confidence levels.",
            Variables: "serviceName,environment,timeRange",
            TargetPersonas: "Engineer,Tech Lead",
            ScopeHint: "incidentId",
            Relevance: "high"),

        new TemplateDefinition(
            Name: "service-health-summary",
            DisplayName: "Service Health Summary",
            Description: "Generates a comprehensive health summary for a service including dependencies, SLIs, and recent incidents.",
            Category: "operations",
            Content: "Provide a comprehensive health summary for service {{serviceName}} in {{environment}}. Include: 1) Current SLI/SLO status, 2) Dependency health and latency, 3) Recent incidents and their resolution, 4) Change frequency and deployment stability, 5) Recommendations for improvement.",
            Variables: "serviceName,environment",
            TargetPersonas: "Engineer,Tech Lead,Architect",
            ScopeHint: "serviceId",
            Relevance: "high"),

        new TemplateDefinition(
            Name: "change-impact-assessment",
            DisplayName: "Change Impact Assessment",
            Description: "Assesses the potential impact of a proposed change on services, contracts, and dependent systems.",
            Category: "governance",
            Content: "Assess the impact of the proposed change to {{serviceName}} in {{environment}}. Evaluate: 1) Blast radius — which services and contracts are affected, 2) Risk level based on change type and service criticality, 3) Required validations before promotion, 4) Recommended rollback strategy, 5) Historical comparison with similar changes.",
            Variables: "serviceName,environment",
            TargetPersonas: "Tech Lead,Architect",
            ScopeHint: "changeId",
            Relevance: "high"),

        // ── Engineering ─────────────────────────────────────────────────
        new TemplateDefinition(
            Name: "api-contract-review",
            DisplayName: "API Contract Review",
            Description: "Reviews an API contract for best practices, breaking changes, and governance compliance.",
            Category: "engineering",
            Content: "Review the API contract for {{contractName}} (version {{version}}). Check for: 1) Breaking changes vs. previous version, 2) Naming conventions and consistency, 3) Error response standardization, 4) Security considerations (authentication, rate limiting), 5) Documentation completeness. Suggest improvements aligned with platform governance policies.",
            Variables: "contractName,version",
            TargetPersonas: "Engineer,Architect",
            ScopeHint: "contractId",
            Relevance: "high"),

        new TemplateDefinition(
            Name: "test-scenario-generation",
            DisplayName: "Test Scenario Generation",
            Description: "Generates test scenarios for a service endpoint based on its contract definition.",
            Category: "engineering",
            Content: "Generate comprehensive test scenarios for the {{endpointMethod}} {{endpointPath}} endpoint of service {{serviceName}}. Include: 1) Happy path scenarios, 2) Error and edge cases, 3) Boundary value tests, 4) Integration test scenarios with dependencies, 5) Performance/load test considerations. Format as structured test cases with expected results.",
            Variables: "endpointMethod,endpointPath,serviceName",
            TargetPersonas: "Engineer",
            ScopeHint: "contractId",
            Relevance: "medium"),

        // ── Management ──────────────────────────────────────────────────
        new TemplateDefinition(
            Name: "team-service-reliability-report",
            DisplayName: "Team Service Reliability Report",
            Description: "Generates a reliability summary for all services owned by a team.",
            Category: "management",
            Content: "Generate a service reliability report for team {{teamName}} covering the period {{period}}. Include: 1) SLO adherence per service, 2) Incident frequency and mean time to resolution, 3) Change success rate, 4) Top recurring issues, 5) Recommendations for reliability improvement. Format for {{targetAudience}} audience.",
            Variables: "teamName,period,targetAudience",
            TargetPersonas: "Tech Lead,Product,Executive",
            ScopeHint: "teamId",
            Relevance: "medium"),

        new TemplateDefinition(
            Name: "executive-operational-summary",
            DisplayName: "Executive Operational Summary",
            Description: "High-level operational summary for executive stakeholders.",
            Category: "management",
            Content: "Provide an executive operational summary for {{period}}. Cover: 1) Platform stability and availability, 2) Key incidents and their business impact, 3) Change velocity and deployment confidence, 4) Cost trends and optimization opportunities, 5) Strategic risks and recommended actions. Use business language, avoid technical jargon.",
            Variables: "period",
            TargetPersonas: "Executive,Product",
            ScopeHint: null,
            Relevance: "medium"),

        // ── Troubleshooting ─────────────────────────────────────────────
        new TemplateDefinition(
            Name: "error-pattern-diagnosis",
            DisplayName: "Error Pattern Diagnosis",
            Description: "Diagnoses recurring error patterns for a service.",
            Category: "troubleshooting",
            Content: "Diagnose the recurring error pattern for service {{serviceName}} with error code/message '{{errorPattern}}'. Analyze: 1) Error frequency and trend, 2) Correlation with recent changes or incidents, 3) Affected endpoints and consumers, 4) Similar patterns in other services, 5) Recommended investigation steps and potential fixes.",
            Variables: "serviceName,errorPattern",
            TargetPersonas: "Engineer,Tech Lead",
            ScopeHint: "serviceId",
            Relevance: "high"),

        // ── Governance ──────────────────────────────────────────────────
        new TemplateDefinition(
            Name: "compliance-audit-checklist",
            DisplayName: "Compliance Audit Checklist",
            Description: "Generates a compliance audit checklist for a service based on governance policies.",
            Category: "governance",
            Content: "Generate a compliance audit checklist for service {{serviceName}} owned by team {{teamName}}. Evaluate: 1) Contract governance (versioning, documentation, approval workflow), 2) Change management compliance (evidence pack, approval gates), 3) Security posture (authentication, authorization, data sensitivity), 4) Operational readiness (runbooks, monitoring, SLOs defined), 5) Ownership and documentation completeness.",
            Variables: "serviceName,teamName",
            TargetPersonas: "Auditor,Platform Admin,Architect",
            ScopeHint: "serviceId",
            Relevance: "high"),

        new TemplateDefinition(
            Name: "cost-optimization-analysis",
            DisplayName: "Cost Optimization Analysis",
            Description: "Analyzes AI token usage patterns for cost optimization recommendations.",
            Category: "governance",
            Content: "Analyze AI token usage for {{scope}} '{{scopeValue}}' over {{period}}. Identify: 1) Usage patterns and peak times, 2) Model selection efficiency (internal vs. external), 3) Token waste (repeated queries, excessive context), 4) Budget adherence and forecast, 5) Recommendations to optimize cost while maintaining quality.",
            Variables: "scope,scopeValue,period",
            TargetPersonas: "Platform Admin,Executive",
            ScopeHint: null,
            Relevance: "medium"),
    };
}
