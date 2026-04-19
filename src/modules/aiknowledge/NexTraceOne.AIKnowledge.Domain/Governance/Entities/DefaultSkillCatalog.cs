using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Catálogo de skills oficiais da plataforma NexTraceOne.
/// Contém as 12 skills System pré-configuradas para operações,
/// engenharia, arquitectura e gestão.
/// </summary>
public static class DefaultSkillCatalog
{
    /// <summary>Retorna todas as skills oficiais da plataforma.</summary>
    public static IReadOnlyList<AiSkill> GetAll() =>
    [
        AiSkill.CreateSystem(
            name: "incident-triage",
            displayName: "Triagem de Incidentes",
            description: "Analisa e triaga incidentes de produção com base em saúde de serviços e mudanças recentes.",
            skillContent: "# Incident Triage Skill\n\nAnalyze the provided incident details and perform triage by checking service health, recent changes, and related incidents. Provide a severity assessment and recommended next steps.",
            tags: ["ops", "incident", "triage"],
            requiredTools: ["search_incidents", "get_service_health", "list_recent_changes"],
            preferredModels: [],
            isComposable: true),

        AiSkill.CreateSystem(
            name: "change-blast-radius",
            displayName: "Análise de Blast Radius",
            description: "Estima o raio de impacto de uma mudança planeada com base em dependências de serviços.",
            skillContent: "# Change Blast Radius Skill\n\nAnalyze the blast radius of the proposed change by examining service dependencies, recent changes, and current health. Provide a risk score and list of potentially affected services.",
            tags: ["ops", "change", "blast-radius", "risk"],
            requiredTools: ["list_recent_changes", "get_service_health", "list_services"],
            preferredModels: [],
            isComposable: true),

        AiSkill.CreateSystem(
            name: "service-health-diagnosis",
            displayName: "Diagnóstico de Saúde de Serviço",
            description: "Diagnostica o estado de saúde de um serviço correlacionando incidentes, contratos e métricas.",
            skillContent: "# Service Health Diagnosis Skill\n\nDiagnose the health of the specified service by correlating incident history, contract anomalies, and health metrics. Provide a diagnosis and recommended actions.",
            tags: ["ops", "health", "diagnosis"],
            requiredTools: ["get_service_health", "search_incidents", "get_contract_details"],
            preferredModels: [],
            isComposable: true),

        AiSkill.CreateSystem(
            name: "contract-lint",
            displayName: "Revisão de Contrato",
            description: "Revê contratos de API para conformidade com boas práticas, consistência e potenciais breaking changes.",
            skillContent: "# Contract Lint Skill\n\nReview the provided API contract for compliance with best practices, naming conventions, versioning standards, and potential breaking changes. Provide a detailed lint report.",
            tags: ["eng", "contract", "lint", "api"],
            requiredTools: ["get_contract_details", "list_services"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "service-scaffold",
            displayName: "Scaffold de Serviço",
            description: "Gera scaffolding de um novo serviço com base em padrões arquitecturais e contratos existentes.",
            skillContent: "# Service Scaffold Skill\n\nGenerate a scaffold for a new service based on existing architectural patterns and contracts. Include recommended structure, dependencies, and initial contract definitions.",
            tags: ["eng", "scaffold", "service"],
            requiredTools: ["list_services", "get_contract_details"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "test-scenario-generator",
            displayName: "Geração de Cenários de Teste",
            description: "Gera cenários de teste abrangentes a partir de contratos e comportamento esperado do serviço.",
            skillContent: "# Test Scenario Generator Skill\n\nGenerate comprehensive test scenarios based on the provided contract and service health data. Include happy path, edge cases, and error scenarios.",
            tags: ["eng", "testing", "scenarios"],
            requiredTools: ["get_contract_details", "get_service_health"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "dependency-risk-scan",
            displayName: "Scan de Risco de Dependências",
            description: "Analisa o grafo de dependências de serviços para identificar riscos e pontos únicos de falha.",
            skillContent: "# Dependency Risk Scan Skill\n\nScan the service dependency graph to identify risks, single points of failure, circular dependencies, and deprecated services. Provide a risk matrix.",
            tags: ["eng", "dependencies", "risk"],
            requiredTools: ["list_services", "get_contract_details"],
            preferredModels: [],
            isComposable: true),

        AiSkill.CreateSystem(
            name: "architecture-fitness",
            displayName: "Avaliação de Architecture Fitness",
            description: "Avalia a conformidade arquitectural do sistema com princípios e decisões de design definidos.",
            skillContent: "# Architecture Fitness Skill\n\nEvaluate the architectural fitness of the system by checking service contracts, dependency patterns, and compliance with architectural decisions. Provide a fitness score and recommendations.",
            tags: ["arch", "architecture", "fitness"],
            requiredTools: ["list_services", "get_contract_details"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "security-owasp-review",
            displayName: "Revisão OWASP de Segurança",
            description: "Revê contratos e serviços à luz das vulnerabilidades OWASP mais comuns.",
            skillContent: "# Security OWASP Review Skill\n\nReview the service contracts and incident history for common OWASP vulnerabilities including injection, broken authentication, and sensitive data exposure. Provide a security assessment.",
            tags: ["arch", "security", "owasp"],
            requiredTools: ["get_contract_details", "list_services", "search_incidents"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "event-schema-design",
            displayName: "Design de Schema de Evento",
            description: "Auxilia na concepção de schemas de eventos Kafka/AsyncAPI com boas práticas e compatibilidade.",
            skillContent: "# Event Schema Design Skill\n\nDesign event schemas following AsyncAPI standards and Kafka best practices. Analyze existing contracts for compatibility and provide schema recommendations with versioning guidance.",
            tags: ["arch", "events", "kafka", "asyncapi", "schema"],
            requiredTools: ["get_contract_details", "list_services"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "tech-debt-quantifier",
            displayName: "Quantificação de Dívida Técnica",
            description: "Quantifica e prioriza a dívida técnica de um portfólio de serviços com base em métricas operacionais.",
            skillContent: "# Tech Debt Quantifier Skill\n\nQuantify and prioritize technical debt across the service portfolio by analyzing incident patterns, service health, and token usage. Provide a debt score and prioritized remediation plan.",
            tags: ["mgmt", "tech-debt", "prioritization"],
            requiredTools: ["list_services", "search_incidents", "get_token_usage_summary"],
            preferredModels: [],
            isComposable: false),

        AiSkill.CreateSystem(
            name: "compliance-mapper",
            displayName: "Mapeamento de Compliance",
            description: "Mapeia serviços e contratos para requisitos de compliance (LGPD, GDPR, SOC2, ISO27001).",
            skillContent: "# Compliance Mapper Skill\n\nMap services and contracts to compliance requirements including LGPD, GDPR, SOC2, and ISO27001. Identify gaps and provide remediation recommendations.",
            tags: ["mgmt", "compliance", "lgpd", "gdpr"],
            requiredTools: ["list_services", "get_contract_details", "search_incidents"],
            preferredModels: [],
            isComposable: false),
    ];
}
