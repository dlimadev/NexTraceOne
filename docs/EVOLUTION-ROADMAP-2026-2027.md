# NexTraceOne — Roadmap de Evolução 2026-2027

> **Data:** Abril 2026
> **Base:** Análise profunda em `docs/DEEP-ANALYSIS-APRIL-2026.md`
> **Pesquisa de mercado:** Backstage (CNCF), Cortex, OpsLevel, Port, Compass (Atlassian), Dynatrace, ServiceNow ITOM, PagerDuty, Datadog
> **Foco:** Estabilização → Hardening → Diferenciação → Expansão

---

## Visão Estratégica

O NexTraceOne compete num mercado em rápida evolução que inclui:
- **Internal Developer Portals (IDP):** Backstage, Cortex, OpsLevel, Port
- **Service Governance:** Atlassian Compass, ServiceNow
- **AIOps & Observability:** Dynatrace, Datadog, New Relic, PagerDuty
- **Change Intelligence:** LaunchDarkly, Split.io, Sleuth

O NexTraceOne diferencia-se por ser a **única plataforma** que combina **todas estas capacidades** num único sistema coerente, com **governança enterprise**, **multi-tenancy**, **AI governada** e **operação self-hosted**.

### O que os clientes enterprise procuram (baseado em pesquisa de mercado)

1. **Single pane of glass** para serviços, contratos, mudanças e operações
2. **Redução de tempo de investigação** de incidentes (correlação automatizada)
3. **Confiança em mudanças de produção** (blast radius, rollback intelligence)
4. **Governança sem fricção** (políticas automáticas, não manuais)
5. **IA contextualizada** (não chat genérico, mas assistência com dados reais)
6. **Métricas DORA** e relatórios executivos
7. **Catálogo de serviços** com ownership claro e scorecards
8. **Conformidade e auditoria** automatizadas (SOC 2, ISO 27001, LGPD/GDPR)
9. **Self-service para developers** com guardrails de governança
10. **Integração com CI/CD existente** (GitHub, GitLab, Azure DevOps, Jenkins)

---

## Fase 0 — ESTABILIZAÇÃO CRÍTICA (Semanas 1-3)

> **Objetivo:** Zero build errors, testes passando, funcionalidades core estáveis

### 0.1 Backend Build & Compilation ⏱️ 1 dia
- [ ] Fix `AiGovernanceEndpointModule.cs:205` — adicionar `using Microsoft.AspNetCore.Http;`
- [ ] Resolver 24 conflitos de assembly version (EF Core 10.0.4 vs 10.0.5)
- [ ] Remover 3 PackageReferences desnecessárias
- [ ] Remover duplicação de xunit em `BuildingBlocks.Security.Tests.csproj`

### 0.2 Frontend Build & Tests ⏱️ 3 dias
- [ ] Fix tipo `GovernanceSummary | undefined` em `DomainDetailPage.tsx` e `TeamDetailPage.tsx`
- [ ] Migrar `onSuccess` do `RunbookBuilderPage.tsx` para pattern correto do TanStack Query v5
- [ ] Criar `TestWrapper` universal com todos os providers (QueryClient, Theme, Environment, Toast)
- [ ] Aplicar TestWrapper — deve resolver ~111 dos 141 testes falhando
- [ ] Fix 8 mocks desatualizados de `aiGovernanceApi`
- [ ] Fix 53 ESLint errors (imports não utilizados, `any` types, hooks deps)

### 0.3 Database Critical ⏱️ 2 dias
- [ ] Gerar migração `InitialCreate` para `TelemetryStoreDbContext` (7 DbSets sem tabelas)
- [ ] Regenerar 6 Designer files em falta (EF tooling)
- [ ] Documentar processo de migração

### 0.4 Outbox Processing ⏱️ 3-5 dias
- [ ] Wire `OutboxProcessorJob` para todos os 24 DbContexts (atualmente apenas 1/24)
- [ ] Ou: documentar intencionalmente quais contexts NÃO precisam de outbox
- [ ] Testar comunicação cross-module via outbox para 3 cenários críticos

---

## Fase 1 — HARDENING (Semanas 4-8)

> **Objetivo:** Production-readiness — segurança, validação, observabilidade

### 1.1 Validação de Input ⏱️ 5 dias
- [ ] Adicionar FluentValidation aos ~30 Commands de escrita mais críticos:
  - Governance: `UpdateDomain`, `ApproveGovernanceWaiver`, `CreateGovernanceWaiver`, `RunComplianceChecks`
  - AuditCompliance: `ApplyRetention`
  - IdentityAccess: `SeedDefaultModuleAccessPolicies`, `SeedDefaultRolePermissions`
- [ ] Template de validador para as restantes ~130 features

### 1.2 Error Handling ⏱️ 3 dias
- [ ] Substituir 4 bare catch blocks em `CanonicalModelBuilder.cs` com logging
- [ ] Adicionar logging às 12+ exceções silenciadas em spec parsers
- [ ] Rever 5 instâncias de null/false silencioso com logging estruturado

### 1.3 Segurança ⏱️ 5 dias
- [ ] Mover password de dev (`ouro18`) para `dotnet user-secrets`
- [ ] Implementar PostgreSQL RLS policies como defesa em profundidade
- [ ] Documentar procedimento de rotação de chaves (JWT + encryption)
- [ ] Configurar CORS por ambiente
- [ ] Encriptar `AuditEvent.Payload` para campos sensíveis
- [ ] Avaliar mover `TenantId` para `AuditableEntity<TId>` base

### 1.4 Implementar Interfaces Críticas ⏱️ 5-8 dias
- [ ] `IEmbeddingProvider` — implementação com Ollama ou OpenAI embeddings para RAG funcional
- [ ] `INotificationTemplateResolver` — resolver templates de notificação (email, webhook)
- [ ] `IPlatformHealthProvider` — agregar saúde da plataforma dos 25 DbContexts
- [ ] `ILegacyEventParser<T>` — pelo menos para formatos JSON e XML genéricos

### 1.5 Eliminar Stubs Remanescentes ⏱️ 5 dias
- [ ] `GetAutomationAction` / `ListAutomationActions` — migrar para BD real com `AutomationActionDbContext`
- [ ] `GetExecutiveDrillDown` — popular ReliabilityScore, ChangeSafety, ContractCoverage a partir dos módulos cross
- [ ] `GetAutomationValidation` — retornar checks reais baseados na workflow definition
- [ ] `GetAutomationWorkflow` — popular Preconditions e ExecutionSteps da BD
- [ ] `GetServiceFinOps` — popular EfficiencyIndicators do `ICostIntelligenceModule`

---

## Fase 2 — FRONTEND COMPLETION (Semanas 6-10)

> **Objetivo:** Todas as páginas conectadas a API real, i18n completo

### 2.1 Páginas de IA (prioridade máxima) ⏱️ 5 dias
- [ ] `AiAssistantPage` (1213 linhas) — conectar a API real de conversas
- [ ] `AiAnalysisPage` (591 linhas) — conectar a análise contextualizada
- [ ] `AgentDetailPage` (563 linhas) — conectar a gestão de agentes

### 2.2 Páginas de Configuração ⏱️ 5 dias
- [ ] `ConfigurationAdminPage` (908 linhas) — conectar a API de configuração
- [ ] `AdvancedConfigurationConsolePage` (839 linhas) — conectar a admin API
- [ ] 5 config pages (Governance, Notification, OperationsFinOps, CatalogContracts, Workflow)

### 2.3 Knowledge & Notifications ⏱️ 3 dias
- [ ] `KnowledgeHubPage`, `OperationalNotesPage`, `KnowledgeDocumentPage` — conectar a Knowledge API
- [ ] `NotificationCenterPage`, `NotificationAnalyticsPage`, `NotificationPreferencesPage` — conectar a Notifications API

### 2.4 Error States ⏱️ 2 dias
- [ ] `ServiceDiscoveryPage` — adicionar error states para 8 useQuery
- [ ] `DelegationPage` — adicionar error states para 5 useQuery
- [ ] `AccessReviewPage` — adicionar error states para 6 useQuery

### 2.5 i18n Completeness ⏱️ 3-5 dias
- [ ] Completar 827 keys em PT-BR
- [ ] Completar 795 keys em PT-PT
- [ ] Completar 999 keys em ES
- [ ] Script de verificação de i18n coverage no CI

### 2.6 Testes ⏱️ 5 dias
- [ ] Adicionar testes para as 40 páginas sem cobertura (pelo menos smoke tests)
- [ ] Atingir 90%+ de testes passando

---

## Fase 3 — DIFERENCIAÇÃO COMPETITIVA (Semanas 11-20)

> **Objetivo:** Funcionalidades que nenhum concorrente oferece como um todo integrado

### 3.1 Service Templates & Scaffolding ⏱️ 8-10 dias
**Inspiração:** Backstage Software Templates

O NexTraceOne já tem o catálogo, contratos e governança. O próximo passo natural é:
- [ ] Template engine para criação de novos serviços com contratos pré-definidos
- [ ] Auto-geração de scaffolding de projeto (.NET, Node, Java) com contratos embedidos
- [ ] Pipeline de criação: template → repositório → contratos → ownership → registro no catálogo
- [ ] Templates versionados e governados

**Valor:** Developers criam serviços conformes desde o primeiro commit.

### 3.2 Dependency Map Intelligence ⏱️ 8-10 dias
**Inspiração:** Cortex + ServiceNow Discovery

O NexTraceOne já tem dependency topology básica. Evoluir para:
- [ ] Auto-discovery de dependências a partir de traces OpenTelemetry
- [ ] Mapa de dependências em tempo real (não apenas estático)
- [ ] Blast radius visual baseado no grafo de dependências
- [ ] Detecção automática de dependências circulares
- [ ] Health propagation — se serviço A depende de B e B está degradado, A é marcado "at risk"

**Valor:** Blast radius real-time, não estimativas estáticas.

### 3.3 Change Confidence Score V2 ⏱️ 5-8 dias
**Inspiração:** Sleuth + Split.io

Evoluir o scoring existente com:
- [ ] Feature flag awareness — integração com LaunchDarkly/Split.io/Unleash
- [ ] Canary deployment tracking — percentagem de rollout como fator de confiança
- [ ] Historical pattern matching — "mudanças similares no passado tiveram X% de falha"
- [ ] Pre-production comparison automática (diff de métricas staging vs production)

**Valor:** Confiança baseada em dados históricos, não apenas análise estática.

### 3.4 AI-Powered Incident Investigation ⏱️ 10-12 dias
**Inspiração:** PagerDuty AIOps + Datadog AI

O NexTraceOne já tem LLM E2E com grounding. Evoluir para:
- [ ] **Auto-triage** — classificação automática de incidentes por severidade baseada em padrões
- [ ] **Root cause suggestion** — análise de timeline de mudanças + métricas + logs para sugerir causa
- [ ] **Mitigation playbook** — seleção automática de runbook baseada em correlação de incidente
- [ ] **Impact assessment** — "este incidente afeta N serviços, M contratos, K clientes"
- [ ] **Similar incident search** — "incidentes semelhantes nos últimos 90 dias"

**Valor:** Tempo de resolução reduzido de horas para minutos.

### 3.5 Compliance as Code ⏱️ 8-10 dias
**Inspiração:** ServiceNow GRC + Vanta

O NexTraceOne já tem audit trail e governance packs. Evoluir para:
- [ ] **Framework templates** — SOC 2, ISO 27001, LGPD/GDPR, PCI-DSS como governance packs pré-configurados
- [ ] **Continuous compliance** — checks automáticos de conformidade em cada mudança
- [ ] **Evidence collection automática** — screenshots, logs, approvals agrupados por controlo
- [ ] **Compliance dashboard** — estado de conformidade por framework, controlo, serviço
- [ ] **Audit-ready reports** — exportação de evidências em formato auditor-friendly

**Valor:** Auditorias que levavam semanas passam a ser contínuas e self-service.

---

## Fase 4 — EXPANSÃO DE ECOSSISTEMA (Semanas 21-30)

> **Objetivo:** Integrações nativas com ecossistema enterprise

### 4.1 CI/CD Integrations Nativas ⏱️ 10-15 dias
- [ ] **GitHub Actions** — GitHub App para ingestão automática de deploy events, PRs, releases
- [ ] **GitLab CI** — webhook receiver para pipeline events
- [ ] **Azure DevOps** — service hook para release gates integrados com NexTraceOne
- [ ] **Jenkins** — plugin para change confidence check como stage
- [ ] **ArgoCD/Flux** — controller para Kubernetes deployments

**Valor:** Zero configuração manual de eventos de deploy.

### 4.2 IDE Extensions ⏱️ 8-10 dias
- [ ] **VS Code Extension** — ver contratos, ownership, change confidence inline
- [ ] **Visual Studio Extension** — mesmas capacidades para ecossistema .NET
- [ ] **JetBrains Plugin** — IntelliJ/Rider para equipas Java/Kotlin

**Valor:** Developers acedem a governança sem sair do IDE.

### 4.3 API Marketplace ⏱️ 10-12 dias
**Inspiração:** SwaggerHub + RapidAPI + Backstage Marketplace

Evoluir o Developer Portal existente para:
- [ ] **Subscrição de APIs** com approval workflow
- [ ] **API keys management** governado
- [ ] **Usage analytics** por consumidor, API, versão
- [ ] **Rate limiting policies** configuráveis por contrato
- [ ] **Sandbox environments** para teste de contratos

**Valor:** Marketplace interno governado, eliminando shadow APIs.

### 4.4 Cost Intelligence V2 ⏱️ 8-10 dias
**Inspiração:** Kubecost + Vantage + Apptio

Evoluir o FinOps existente para:
- [ ] **Cloud cost correlation** — AWS/Azure cost por serviço correlacionado com mudanças
- [ ] **Anomaly detection** — alertas de custo fora do padrão
- [ ] **Budget forecasting** — projeção de custos baseada em tendência + mudanças planeadas
- [ ] **Efficiency recommendations** — "serviço X gasta 40% mais que serviços similares"
- [ ] **Showback reports** — custo por equipa, domínio, serviço para accountability

**Valor:** FinOps contextualizado por serviço, não apenas dashboards de custo cloud genéricos.

### 4.5 Multi-Cluster & Multi-Cloud ⏱️ 10-15 dias
- [ ] **Kubernetes integration** — auto-discovery de serviços via CRDs/annotations
- [ ] **Multi-cluster view** — estado de serviços across clusters
- [ ] **Cloud-agnostic** — AWS, Azure, GCP, on-premises num único painel
- [ ] **Edge deployment support** — telemetria de ambientes edge

---

## Fase 5 — DIFERENCIAÇÃO AVANÇADA (Semanas 31-40+)

> **Objetivo:** Funcionalidades que posicionam o NexTraceOne como líder de mercado

### 5.1 Predictive Intelligence ⏱️ 15-20 dias
- [ ] **Failure prediction** — "baseado em padrões, este serviço tem 73% probabilidade de incidente nas próximas 24h"
- [ ] **Capacity planning** — "com a tendência atual, serviço X atinge saturação em 2 semanas"
- [ ] **Change risk prediction** — ML model treinado com histórico de mudanças vs incidentes
- [ ] **SLO burn rate alerts** — "ao ritmo atual, SLO será violado em 4 horas"

### 5.2 Developer Experience Score ⏱️ 8-10 dias
**Inspiração:** DX Core 4 + SPACE framework

- [ ] **Developer survey automation** — questionários periódicos integrados na plataforma
- [ ] **Productivity metrics** — cycle time, deployment frequency por developer/equipa
- [ ] **Cognitive load measurement** — número de serviços, contratos, dependências por equipa
- [ ] **Toil tracking** — tempo gasto em tarefas repetitivas vs desenvolvimento
- [ ] **Developer NPS** — satisfação com ferramentas, processos, plataforma

### 5.3 GraphQL Federation Gateway ⏱️ 10-15 dias
- [ ] Gateway GraphQL federado que expõe o catálogo completo do NexTraceOne
- [ ] Schema stitching automático entre módulos
- [ ] Subscriptions para eventos real-time (mudanças, incidentes, deploys)
- [ ] SDK para integração com ferramentas externas

### 5.4 Observability Correlation Engine ⏱️ 15-20 dias
- [ ] **Trace-to-change correlation** — ligar traces a mudanças específicas
- [ ] **Log anomaly detection** — detecção de padrões anómalos em logs pós-deploy
- [ ] **Metric correlation** — correlação automática entre métricas de diferentes serviços
- [ ] **Topology-aware alerting** — alertas inteligentes baseados no grafo de dependências

### 5.5 Governance Policy Engine V2 ⏱️ 10-15 dias
- [ ] **Policy as Code** — políticas definíveis em YAML/JSON, versionadas no repositório
- [ ] **Policy simulation** — "se aplicar esta política, X serviços ficam non-compliant"
- [ ] **Gradual enforcement** — policies em modo warning antes de blocking
- [ ] **Exception management** — waivers com expiração automática e audit trail

---

## Cronograma Resumido

```
             Abr  Mai  Jun  Jul  Ago  Set  Out  Nov  Dez  Jan  Fev  Mar
             2026 2026 2026 2026 2026 2026 2026 2026 2026 2027 2027 2027
Fase 0       ████
Fase 1            ████████
Fase 2            ████████████
Fase 3                      ████████████████████
Fase 4                                     ████████████████████
Fase 5                                                    ████████████████
```

---

## KPIs de Sucesso por Fase

| Fase | KPI | Meta |
|------|-----|------|
| 0 | Build errors | 0 |
| 0 | Testes passando | 95%+ |
| 1 | Features com validators | 85%+ |
| 1 | Catch blocks com logging | 100% |
| 1 | Outbox processors ativos | 24/24 |
| 2 | Páginas com API real | 100% |
| 2 | i18n coverage (todos idiomas) | 95%+ |
| 2 | Páginas com testes | 90%+ |
| 3 | Tempo médio de resolução de incidentes | -40% |
| 3 | Compliance checks automáticos | 100% dos frameworks configurados |
| 4 | CI/CD integrations | ≥3 plataformas |
| 4 | IDE extensions | ≥2 IDEs |
| 5 | Prediction accuracy | >70% para failure prediction |

---

## Prioridades Baseadas em Valor para o Cliente

### Impacto Imediato (Fase 0-1)
1. **Estabilidade** — sem build errors, testes passando, sem stubs silenciosos
2. **Segurança** — RLS, rotação de chaves, validação de input

### Impacto Alto (Fase 2-3)
3. **Developer self-service** — todas as páginas funcionais, templates de serviço
4. **Incident resolution time** — AI investigation, auto-triage, runbook selection
5. **Compliance automation** — evidence collection, continuous compliance

### Impacto Diferenciador (Fase 4-5)
6. **Zero-touch deploy events** — integração nativa CI/CD
7. **Predictive intelligence** — previsão de incidentes, capacity planning
8. **IDE integration** — governança no fluxo de trabalho do developer
9. **API Marketplace** — catálogo vivo com subscrição e analytics
10. **Cost intelligence** — FinOps contextualizado por mudança e serviço

---

## Análise Competitiva — Posicionamento

| Capacidade | Backstage | Cortex | OpsLevel | ServiceNow | PagerDuty | Datadog | **NexTraceOne** |
|------------|-----------|--------|----------|------------|-----------|---------|-----------------|
| Service Catalog | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Contract Governance | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Change Intelligence | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Incident Management | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| AI Governance | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Audit Blockchain | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Multi-tenancy | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Self-hosted | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅** |
| DORA Metrics | Plugin | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Service Scorecards | Plugin | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| Compliance as Code | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ✅ (com Fase 3) |
| Predictive AI | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ (com Fase 5) |
| FinOps | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |

### Diferenciação Única do NexTraceOne
1. **Contract Governance** — nenhum concorrente oferece governança de contratos REST/SOAP/Kafka/Event como first-class citizen
2. **AI Governance** — modelo registry, quotas, policies, audit de uso — nenhum concorrente tem isto integrado
3. **Audit Blockchain** — trilha de auditoria com SHA-256 chain — enterprise-grade tamper-proof
4. **FinOps contextualizado** — custo correlacionado com serviço, mudança, incidente — não existe em nenhum concorrente
5. **Tudo self-hosted** — enterprise pode correr tudo on-premises, ao contrário de Cortex, OpsLevel, PagerDuty
