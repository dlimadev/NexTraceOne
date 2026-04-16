using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Configuration.Infrastructure.Persistence;

namespace NexTraceOne.Configuration.Infrastructure.Seed;

/// <summary>
/// Seeder idempotente para definições de feature flags da plataforma NexTraceOne.
/// Apenas insere definições que ainda não existem (verificação por chave).
/// As feature flags da wave W00 (legacy) já são inseridas via migration SQL;
/// este seeder complementa com todas as flags de produto para as restantes capacidades.
///
/// Comportamento em primeira execução: insere todas as definições.
/// Comportamento em re-execuções: ignora definições já existentes (IsNoOp).
/// </summary>
public sealed class FeatureFlagDefinitionSeeder(ConfigurationDbContext dbContext)
    : IFeatureFlagDefinitionSeeder
{
    /// <inheritdoc />
    public Task<SeedingResult> SeedAsync(CancellationToken cancellationToken = default)
        => SeedDefaultDefinitionsAsync(dbContext, cancellationToken);

    /// <summary>
    /// Insere as definições de feature flags iniciais se ainda não existirem.
    /// </summary>
    public static async Task<SeedingResult> SeedDefaultDefinitionsAsync(
        ConfigurationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existingKeys = await dbContext.FeatureFlagDefinitions
            .Select(d => d.Key)
            .ToHashSetAsync(cancellationToken);

        var definitions = BuildDefaultDefinitions();
        var added = 0;
        var skipped = 0;

        foreach (var definition in definitions)
        {
            if (existingKeys.Contains(definition.Key))
            {
                skipped++;
            }
            else
            {
                await dbContext.FeatureFlagDefinitions.AddAsync(definition, cancellationToken);
                existingKeys.Add(definition.Key);
                added++;
            }
        }

        if (added > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new SeedingResult(added, skipped);
    }

    private static readonly ConfigurationScope[] SystemTenantEnv =
    [
        ConfigurationScope.System,
        ConfigurationScope.Tenant,
        ConfigurationScope.Environment,
    ];

    private static readonly ConfigurationScope[] SystemTenant =
    [
        ConfigurationScope.System,
        ConfigurationScope.Tenant,
    ];

    private static readonly ConfigurationScope[] AllScopes =
    [
        ConfigurationScope.System,
        ConfigurationScope.Tenant,
        ConfigurationScope.Environment,
        ConfigurationScope.Role,
        ConfigurationScope.Team,
        ConfigurationScope.User,
    ];

    private static List<FeatureFlagDefinition> BuildDefaultDefinitions() =>
    [
        // ── AI / Assistente ───────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "ai.assistant.enabled",
            displayName: "AI Assistant",
            allowedScopes: AllScopes,
            description: "Ativa o assistente de IA para todos os utilizadores com acesso permitido pela política de modelo.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "ai.copilot.enabled",
            displayName: "AI Copilot (Web)",
            allowedScopes: AllScopes,
            description: "Ativa o copiloto IA embebido na interface web — análise contextual assistida por IA.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "ai.agents.enabled",
            displayName: "AI Agents",
            allowedScopes: SystemTenantEnv,
            description: "Ativa agentes IA especializados (contratos, change impact, investigação operacional).",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "ai.model-registry.enabled",
            displayName: "AI Model Registry",
            allowedScopes: SystemTenant,
            description: "Ativa o registo e gestão de modelos de IA disponíveis na plataforma.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "ai.external-providers.enabled",
            displayName: "External AI Providers",
            allowedScopes: SystemTenant,
            description: "Permite o uso de provedores externos de IA (OpenAI, Azure OpenAI, Anthropic, etc.) conforme política.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "ai.ide-extensions.enabled",
            displayName: "IDE Extensions",
            allowedScopes: AllScopes,
            description: "Ativa integrações IDE (VS Code, Visual Studio) com o NexTraceOne para aceleração de desenvolvimento.",
            defaultEnabled: false,
            isEditable: true),

        // ── Catálogo de Serviços ─────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "catalog.service-topology.enabled",
            displayName: "Service Topology",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a visualização de topologia de dependências entre serviços.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "catalog.service-scorecard.enabled",
            displayName: "Service Scorecard",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o scorecard de qualidade e maturidade por serviço.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "catalog.service-lifecycle.enabled",
            displayName: "Service Lifecycle",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a gestão de ciclo de vida de serviços (Draft, Active, Deprecated, Decommissioned).",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "catalog.custom-fields.enabled",
            displayName: "Service Custom Fields",
            allowedScopes: SystemTenant,
            description: "Permite definir e gerir campos personalizados por serviço.",
            defaultEnabled: true,
            isEditable: true),

        // ── Contratos ────────────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "contracts.rest.enabled",
            displayName: "REST API Contracts",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a gestão de contratos REST / OpenAPI na plataforma.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "contracts.soap.enabled",
            displayName: "SOAP Contracts",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a gestão de contratos SOAP / WSDL na plataforma.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "contracts.events.enabled",
            displayName: "Event Contracts (AsyncAPI / Kafka)",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a gestão de contratos de eventos, AsyncAPI e tópicos Kafka.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "contracts.semantic-diff.enabled",
            displayName: "Semantic Contract Diff",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o diff semântico entre versões de contratos para deteção de breaking changes.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "contracts.studio.enabled",
            displayName: "Contract Studio",
            allowedScopes: AllScopes,
            description: "Ativa o editor visual de contratos com assistência de IA.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "contracts.publication-center.enabled",
            displayName: "Contract Publication Center",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o workflow de publicação e aprovação de contratos.",
            defaultEnabled: true,
            isEditable: true),

        // ── Change Intelligence ──────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "changes.blast-radius.enabled",
            displayName: "Blast Radius Analysis",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a análise de blast radius para mudanças em produção.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "changes.production-confidence.enabled",
            displayName: "Production Change Confidence Score",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o scoring de confiança para mudanças a promover para produção.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "changes.rollback-intelligence.enabled",
            displayName: "Rollback Intelligence",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a análise inteligente de rollback com sugestões baseadas em histórico.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "changes.release-calendar.enabled",
            displayName: "Release Calendar",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o calendário de releases com janelas de mudança e restrições por ambiente.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "changes.evidence-pack.enabled",
            displayName: "Evidence Pack",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a geração automática de pacotes de evidência para mudanças.",
            defaultEnabled: true,
            isEditable: true),

        // ── Operações ───────────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "operations.incident-correlation.enabled",
            displayName: "Incident-Change Correlation",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a correlação automática entre incidentes e mudanças recentes.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "operations.runbooks.enabled",
            displayName: "Runbooks",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a gestão e execução de runbooks operacionais.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "operations.aiops-insights.enabled",
            displayName: "AIOps Insights",
            allowedScopes: SystemTenantEnv,
            description: "Ativa insights operacionais assistidos por IA (correlação, causa raiz, mitigação).",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "operations.post-change-verification.enabled",
            displayName: "Post-Change Verification",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a verificação automática de saúde pós-change.",
            defaultEnabled: true,
            isEditable: true),

        // ── Knowledge Hub ────────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "knowledge.hub.enabled",
            displayName: "Knowledge Hub",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o hub de documentação e conhecimento operacional.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "knowledge.search.enabled",
            displayName: "Global Search / Command Palette",
            allowedScopes: AllScopes,
            description: "Ativa a pesquisa global e command palette (Ctrl+K) no produto.",
            defaultEnabled: true,
            isEditable: true),

        // ── Observabilidade ──────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "observability.trace-explorer.enabled",
            displayName: "Trace Explorer",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o explorador de traces OpenTelemetry contextualizado por serviço e mudança.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "observability.log-explorer.enabled",
            displayName: "Log Explorer",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o explorador de logs estruturados contextualizado por serviço.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "observability.dora-metrics.enabled",
            displayName: "DORA Metrics",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o dashboard de métricas DORA (Deployment Frequency, Lead Time, MTTR, CFR).",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "observability.canary-tracking.enabled",
            displayName: "Canary Deployment Tracking",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o acompanhamento de rollouts canary com comparação de métricas stable vs canary.",
            defaultEnabled: false,
            isEditable: true),

        // ── FinOps ───────────────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "finops.contextual.enabled",
            displayName: "Contextual FinOps",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o dashboard de FinOps contextualizado por serviço, equipa e mudança.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "finops.waste-detection.enabled",
            displayName: "Waste Detection",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a deteção automática de desperdício de recursos por serviço e ambiente.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "finops.greenops.enabled",
            displayName: "GreenOps",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a monitorização de footprint de carbono e recomendações de eficiência energética.",
            defaultEnabled: false,
            isEditable: true),

        // ── Governance / Compliance ──────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "governance.risk-center.enabled",
            displayName: "Risk Center",
            allowedScopes: SystemTenantEnv,
            description: "Ativa o centro de risco com análise de exposição por serviço e domínio.",
            defaultEnabled: true,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "governance.compliance-packs.enabled",
            displayName: "Compliance Packs",
            allowedScopes: SystemTenant,
            description: "Ativa a gestão de packs de compliance (SOC2, ISO27001, GDPR, LGPD, PCI-DSS).",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "governance.audit-trail.enabled",
            displayName: "Audit Trail",
            allowedScopes: SystemTenant,
            description: "Ativa o registo imutável de auditoria para todas as ações relevantes da plataforma.",
            defaultEnabled: true,
            isEditable: false),

        // ── Platform Admin / Self-Hosted ─────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "platform.multi-tenant.enabled",
            displayName: "Multi-Tenant Schema Isolation",
            allowedScopes: SystemTenant,
            description: "Ativa o isolamento schema-per-tenant no PostgreSQL para separação física de dados.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "platform.saml-sso.enabled",
            displayName: "SAML SSO",
            allowedScopes: SystemTenant,
            description: "Ativa a autenticação via SAML 2.0 com identity providers externos.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "platform.mtls.enabled",
            displayName: "Mutual TLS (mTLS)",
            allowedScopes: SystemTenant,
            description: "Ativa autenticação mútua TLS para comunicação serviço-a-serviço.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "platform.elasticsearch.enabled",
            displayName: "Elasticsearch Integration",
            allowedScopes: SystemTenant,
            description: "Ativa a integração com Elasticsearch para pesquisa e analytics avançados.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "platform.greenops.carbon-reporting.enabled",
            displayName: "Carbon Footprint Reporting",
            allowedScopes: SystemTenant,
            description: "Ativa relatórios de pegada de carbono integrados com dados de infraestrutura.",
            defaultEnabled: false,
            isEditable: true),

        // ── Integrações ──────────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "integrations.gitlab.enabled",
            displayName: "GitLab Integration",
            allowedScopes: SystemTenant,
            description: "Ativa a integração com GitLab para ingestão de eventos de deploy e pipeline.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "integrations.github.enabled",
            displayName: "GitHub Integration",
            allowedScopes: SystemTenant,
            description: "Ativa a integração com GitHub para ingestão de eventos de deploy e pipeline.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "integrations.jenkins.enabled",
            displayName: "Jenkins Integration",
            allowedScopes: SystemTenant,
            description: "Ativa a integração com Jenkins para ingestão de eventos de build e deploy.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "integrations.azure-devops.enabled",
            displayName: "Azure DevOps Integration",
            allowedScopes: SystemTenant,
            description: "Ativa a integração com Azure DevOps para ingestão de eventos de pipeline e release.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "integrations.kafka.enabled",
            displayName: "Kafka / Event Streaming",
            allowedScopes: SystemTenantEnv,
            description: "Ativa a integração com Kafka para monitorização de tópicos e contratos de eventos.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "integrations.webhooks.enabled",
            displayName: "Webhooks",
            allowedScopes: SystemTenant,
            description: "Ativa o envio de webhooks para sistemas externos em resposta a eventos da plataforma.",
            defaultEnabled: true,
            isEditable: true),

        // ── Product Analytics ────────────────────────────────────────────────────

        FeatureFlagDefinition.Create(
            key: "product-analytics.enabled",
            displayName: "Product Analytics",
            allowedScopes: SystemTenant,
            description: "Ativa a recolha e análise de métricas de adoção e uso do produto.",
            defaultEnabled: false,
            isEditable: true),

        FeatureFlagDefinition.Create(
            key: "product-analytics.dora-admin.enabled",
            displayName: "DORA Admin Dashboard",
            allowedScopes: SystemTenant,
            description: "Ativa o dashboard DORA para administradores de plataforma com visão agregada.",
            defaultEnabled: false,
            isEditable: true),
    ];
}
