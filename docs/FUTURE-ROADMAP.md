# NexTraceOne — Future Roadmap

> **Data:** Abril 2026  
> **Estado atual:** ~98% implementado — todos os módulos core estão READY  
> **Referência:** [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md)

---

## Sumário

Este documento consolida **todas as funcionalidades planeadas** para waves futuros do NexTraceOne. Todas as funcionalidades listadas aqui são **evolução futura** — não são gaps da implementação atual.

O NexTraceOne está operacional com 12 módulos backend, 130+ páginas frontend, 99+ endpoints, 296+ entidades de domínio, 154+ migrações e 2000+ testes.

---

## 1. Novos Tipos de Contrato

### 1.1 GraphQL Contracts
- **Estado:** Enum `ContractType.GraphQL` existe; implementação de parsing/validação pendente
- **Escopo:** Parser de schema GraphQL, validação de breaking changes, visual builder
- **Dependência:** Decisão de design sobre representação de queries/mutations/subscriptions
- **Referência:** GAP-CTR-01

### 1.2 Protobuf / gRPC Contracts
- **Estado:** Enum `ContractType.Protobuf` existe; implementação de parsing/validação pendente
- **Escopo:** Parser de `.proto` files, validação de compatibilidade (wire format), visual builder
- **Dependência:** Decisão sobre suporte de evolução de schema (field renumbering, reserved fields)
- **Referência:** GAP-CTR-02

---

## 2. IDE Extensions

### 2.1 VS Code Extension
- **Escopo:** Ver contratos, ownership, change confidence inline no editor
- **Capacidades:** Service lookup, contract preview, change risk indicator, AI assistant
- **Dependência:** API pública estável + autenticação via token

### 2.2 Visual Studio Extension
- **Escopo:** Mesmas capacidades para ecossistema .NET
- **Dependência:** VSIX packaging + API token management

### 2.3 JetBrains Plugin (IntelliJ/Rider)
- **Escopo:** Mesmas capacidades para equipas Java/Kotlin/.NET
- **Dependência:** IntelliJ Platform SDK

---

## 3. Integrações Profundas

### 3.1 CI/CD Deep Integration
- **Estado:** Metadata capture funcional via webhook normalizers (GitHub, GitLab, Azure DevOps, Jenkins, ArgoCD)
- **Evolução:** Real pipeline parsing, status tracking, deployment correlation avançada
- **Referência:** GAP-INT-01

### 3.2 Real Kafka Producer/Consumer
- **Estado:** Modelo de domínio completo; real producer/consumer pendente
- **Escopo:** Integração com Apache Kafka para event streaming real
- **Dependência:** Infraestrutura message broker (Kafka cluster)
- **Referência:** GAP-INT-02

### 3.3 External Queue Consumer
- **Estado:** Pipeline de ingestion funcional; consumer externo pendente
- **Escopo:** Worker process para consumir eventos de fila externa (RabbitMQ, Azure Service Bus, SQS)
- **Dependência:** Planeado junto com Kafka integration
- **Referência:** GAP-INT-04

### 3.4 SDK Externo
- **Escopo:** SDK para integração com ferramentas externas (CLI, scripts, automação)
- **Dependência:** SDK packaging pipeline + versioning strategy

---

## 4. Autenticação Avançada

### 4.1 SAML Protocol Handlers ✅ IMPLEMENTADO (Abril 2026)
- **Estado:** ✅ `ISamlProvider` + `NullSamlProvider` + `ConfigurationSamlProvider` implementados; SAML aparece em `/admin/system-health` como 5º provider opcional. Fluxo `StartSamlLogin` / `CompleteSamlLogin` funcional em modo controlado.
- **Pendente futuro:** E2E SAML SSO com Playwright + mock IdP real (ADFS/Okta/PingFederate). Necessita sessão dedicada com docker-compose IdP stack.
- **Dependência:** Integração com identity provider específico para testes E2E
- **Referência:** GAP-AI-08, DEG-11 (HONEST-GAPS.md Nível A′ → promovido a A)

---

## 5. DevOps & Build Pipeline

### 5.1 Assembly/Artifact Signing
- **Escopo:** Assinatura digital de assemblies e artefactos de build
- **Dependência:** Certificate provisioning + build pipeline tooling
- **Referência:** GAP-SEC-03

---

## 6. Qualidade & Testes

### 6.1 Frontend Unit Tests para Contract Builders ✅ IMPLEMENTADO (Abril 2026)
- **Estado:** ✅ 6 novos ficheiros de teste — 69+ casos passando
  - `VisualSoapBuilderValidation.test.ts` — 11 casos (validateSoapBuilder)
  - `VisualEventBuilderValidation.test.ts` — 14 casos (validateEventBuilder)
  - `WorkserviceBuilderValidation.test.ts` — 13 casos (validateWorkserviceBuilder)
  - `WebhookBuilderValidation.test.ts` — 11 casos (validateWebhookBuilder)
  - `SharedSchemaBuilderValidation.test.ts` — 9 casos (validateSharedSchemaBuilder)
  - `ServiceRegistrationWizard.test.tsx` — 7 casos (step navigation, validation, submit)
- **Total suite:** 1127 testes passando
- **Referência:** GAP-CTR-09

### 6.2 E2E Tests para Fluxos de Contrato ✅ IMPLEMENTADO (Abril 2026)
- **Estado:** ✅ 3 novos ficheiros Playwright criados
  - `contract-versioning-flows.spec.ts` — versioning, lifecycle badges, draft creation
  - `contract-approval-flows.spec.ts` — draft/review/approved/rejected state display
  - `contract-deprecation-flows.spec.ts` — deprecated contract list, detail, health metrics
- **Referência:** GAP-CTR-10

---

## 7. Sandbox & Playground

### 7.1 Contract Sandbox Environments
- **Estado:** `PlaygroundSession` já existe; sandbox completo com mocking requer infra adicional
- **Escopo:** Ambientes isolados para teste de contratos com mock servers
- **Dependência:** Infraestrutura de containerização para sandboxes temporários

---

## 8. AI Evoluções

### 8.1 Agentes AI Especializados
- **Escopo:** Agentes dedicados: Dependency Advisor, Architecture Fitness, Doc Quality
- **Dependência:** Maturidade do framework de agentes existente

### 8.2 NLP-based Model Routing
- **Estado:** Keyword heuristics funcional; NLP-based routing como evolução
- **Escopo:** Routing inteligente de prompts para modelos especializados

### 8.3 Cross-Module Grounding Avançado
- **Estado:** Grounding básico funcional via `IKnowledgeModule`
- **Escopo:** Enriquecer contexto de IA com entidades de todos os módulos

### 8.4 ML-based Incident Correlation
- **Estado:** Heurísticas básicas (timestamp + service matching)
- **Escopo:** Correlação avançada baseada em ML/NLP

---

## 9. Compliance as Code (Evolução)

### 9.1 Advanced Compliance Packs
- **Escopo:** PCI-DSS, HIPAA, SOC2 packs dedicados com checks automatizados
- **Dependência:** Expertise regulatória + partnership com consultoras

---

## 10. FinOps Avançado

### 10.1 FinOps com Dados de Custo Real
- **Escopo:** Integração direta com AWS Cost Explorer, Azure Cost Management, GCP Billing
- **Dependência:** Credenciais cloud + data pipeline

---

## 11. Infraestrutura

### 11.1 Kubernetes Deployment
- **Estado:** Docker Compose funcional; IIS suportado
- **Escopo:** Helm charts, horizontal scaling, service mesh integration

### 11.2 ClickHouse para Observability
- **Estado:** Elasticsearch como provider padrão; ClickHouse como alternativa
- **Escopo:** ClickHouse como storage analítico para workloads de observabilidade de alto volume

---

## 12. Legacy/Mainframe Waves

> Plano detalhado em `docs/legacy/` (WAVE-00 a WAVE-12)

- **WAVE-00:** Strategy & Foundation — layout de domínio para core systems legados
- **WAVE-01:** Catalog Foundation — IBM Z, COBOL, CICS, IMS, DB2, MQ
- **WAVE-02:** Input Formats & Telemetry Ingestion
- **WAVE-03:** Normalization & Correlation
- **WAVE-04:** Contract Governance para legacy
- **WAVE-05:** Hybrid Graph
- **WAVE-06:** Change Intelligence para legacy
- **WAVE-07:** Batch Intelligence
- **WAVE-08:** Messaging Intelligence
- **WAVE-09:** AI Assistive para legacy
- **WAVE-10:** Workflow & Policies
- **WAVE-11:** Frontend Enterprise
- **WAVE-12:** Security Readiness

---

## 13. EF Core Designer Files

### 13.1 Regenerar Designer Files
- **Estado:** 13 Designer files em falta (requerem `dotnet ef` com PostgreSQL ativo)
- **Módulos afectados:**
  - AuditCompliance (1): `P7_4_AuditCorrelationId`
  - IdentityAccess (1): `AddTenantOrganizationFields`
  - Catalog (3): `P52B_DeveloperSurveys`, `W04_LegacyContractGovernance`, `AddCatalogSearchGinIndexes`, `P52_DeveloperExperienceScore`
  - Configuration (1): `AddPhase3To8ConfigurationTables`
  - OperationalIntelligence (4): `P51_PredictiveIntelligence`, `W01_TelemetryStoreFoundation`, `AddCustomCharts`, `AddChaosExperiments`
  - AIKnowledge (2): `P05_Innovation`, `P04_BackendEnhancements`
- **Comando:** `dotnet ef migrations add <Name> --project <InfraProject> --startup-project src/platform/NexTraceOne.ApiHost`
- **Nota:** Requer ambiente local com PostgreSQL; não executável em sandbox CI

---

## Priorização Recomendada

| Prioridade | Funcionalidade | Impacto |
|------------|---------------|---------|
| ✅ **DONE** | Frontend Unit Tests (6.1) | Qualidade e confiança em contract builders |
| ✅ **DONE** | E2E Tests (6.2) | Cobertura de fluxos críticos |
| **Alta** | EF Designer Files (13.1) | Completar tooling de migrações |
| **Média** | IDE Extensions (2.x) | Developer experience |
| **Média** | GraphQL/Protobuf Contracts (1.x) | Expansão de tipos de contrato |
| **Média** | SAML (4.1) | Enterprise SSO |
| **Média** | Kafka Integration (3.2) | Event streaming real |
| **Baixa** | SDK Externo (3.4) | Automação e integração |
| **Baixa** | Contract Sandbox (7.1) | Testing avançado |
| **Baixa** | Advanced Compliance (9.1) | Packs regulatórios |
| **Roadmap** | Legacy/Mainframe (12.x) | Expansão para core systems |
| **Roadmap** | Kubernetes (11.1) | Infraestrutura de deployment |

---

## 14. Evolução de Customização da Plataforma

> Plano detalhado em [`PLATFORM-CUSTOMIZATION-EVOLUTION.md`](./PLATFORM-CUSTOMIZATION-EVOLUTION.md)

Customizações que o utilizador pode realizar **sem alterar a identidade visual**:

- **Fase 1:** ✅ Saved views, bookmarks, default scope, timezone, colunas visíveis, paginação
- **Fase 2:** ✅ Dashboard templates por persona, custom charts, widget de notas, cloning, drill-down
- **Fase 3:** ✅ Watch lists, quiet hours, custom alert rules, digest personalizado
- **Fase 4:** ✅ Custom tags, custom metadata fields, custom taxonomies
- **Fase 5:** ✅ Custom checklists, automation rules, custom contract templates
- **Fase 6:** ✅ Scheduled reports, export configurável, saved report templates
- **Fase 7:** ✅ Custom AI prompts, AI behavior preferences, AI knowledge scope
- **Fase 8:** ✅ Custom webhook payloads, API keys management, custom integration mappings

---

## Notas

1. **Este documento substitui:** ROADMAP.md, EVOLUTION-ROADMAP-2026-2027.md, CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md, SERVICE-CREATION-STUDIO-PLAN.md
2. **Documentos de referência mantidos:** PRODUCT-VISION.md, ARCHITECTURE-OVERVIEW.md, docs/adr/, docs/legacy/, docs/security/, docs/deployment/, docs/runbooks/, docs/observability/, docs/user-guide/
3. **Licensing module** foi removido da solução e não consta neste roadmap
4. **Convites in-app** foram removidos por decisão de produto — onboarding é SSO-first. Ver `docs/HONEST-GAPS.md` (OOS-02).
5. **~98% do produto está implementado** — este roadmap cobre os ~2% restantes + evolução futura. A lista consolidada de gaps abertos está em [HONEST-GAPS.md](./HONEST-GAPS.md).
6. **Customização da plataforma:** Plano detalhado em [PLATFORM-CUSTOMIZATION-EVOLUTION.md](./PLATFORM-CUSTOMIZATION-EVOLUTION.md)
