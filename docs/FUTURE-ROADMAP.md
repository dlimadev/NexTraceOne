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
- **E2E:** ✅ `src/frontend/e2e/saml-sso-flows.spec.ts` — 14 testes Playwright cobrindo admin config page, SAML initiation, ACS callback e end-to-end flow com mock IdP via route interception (ACT-022 concluído Abril 2026).
- **Futuro:** Testes com IdP real (ADFS/Okta/PingFederate) em docker-compose stack — apenas para certificação enterprise, não bloqueante para v1.0.0.
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

## 15. Waves Suplementares de Evolução (pós-v1.0.0 → v1.x → v2.0)

> **Contexto**: plano aprovado em Abril 2026 (ver [ADR-007](./adr/007-data-contracts.md), [ADR-008](./adr/008-change-confidence-score-v2.md), [ADR-009](./adr/009-ai-evaluation-harness.md)).
> Esta secção **complementa** — não duplica — os itens já listados acima. Onde há sobreposição, a secção existente mantém-se como fonte da verdade e esta secção limita-se a referenciá-la.

O plano está organizado em quatro waves ordenadas por valor marginal por unidade de esforço e por aderência à "Regra mestra de evolução" (capítulo 4 das Copilot Instructions).

### Wave A — Depth (aprofundar o core existente)

Prioridade **máxima**. Reforça pilares já fortes sem criar módulos novos.

#### A.1 Change Intelligence — de reativo para preditivo

- **Predictive Blast Radius v2** — evoluir o heurístico atual (ownership + dependência) para modelo que combina `BlastRadius` + traces OTel históricos + incidentes passados sobre o par (serviço, contrato). Estima probabilidade de regressão **por contrato consumido**, não só por serviço.
- **Change Confidence Score 2.0** — decomposição explicável em sub-scores (TestCoverage, ContractStability, HistoricalRegression, BlastSurface, DependencyHealth, CanarySignal, PreProdDelta) com citações auditáveis. Ver [ADR-008](./adr/008-change-confidence-score-v2.md).
- **Promotion Readiness Delta** — comparar baselines `RuntimeIntelligence` entre dev → staging → prod para o mesmo `ReleaseIdentity`, expor delta visível no `ReleaseTrain` e nos Promotion Gates. Concretiza o capítulo 9.3 das Copilot Instructions ("ambientes não produtivos como fonte crítica para prevenir falhas em produção").
- **TTD / TTR KPIs por equipa** — cruzar `CostIntelligence` × incidentes, já existem hooks; falta produto de UX por persona.

#### A.2 Contract Governance — contratos como dados vivos

- **Data Contracts** — novo `ContractType.DataContract` para tabelas/vistas/streams analíticos (owner, SLA de frescura, schema, PII classification). Ver [ADR-007](./adr/007-data-contracts.md).
- **Contract-to-Consumer Tracking real via OTel** — persistir `ContractConsumerInventory` (quem chamou, com que versão, em que ambiente, com que frequência) derivado de traces já ingeridos. Habilita deprecation governance útil.
- **Contract Linting Marketplace** — pacotes Spectral oficiais distribuídos via `ConfigurationDefinitionSeeder` (enterprise, security, accessibility, internal-platform), ativáveis por tenant.
- **Breaking Change Proposal Workflow** — workflow formal antes de publicar v2 com quebras: consulta consumidores, janela de migração, `DeprecationPlan` automático.

#### A.3 Service Catalog — ownership vivo e scorecards

- **Ownership Drift Detection** — alertar serviços com N dias sem merge, sem on-call atualizado, sem owner presente em `SecurityEvents`.
- **Service Maturity v2** — eixos explícitos (Reliability, Security, Observability, Documentation, Contract Health, DevEx) comparados contra "golden path" da organização, não apenas peers.
- **Tier-based SLO enforcement** — `ServiceTier` (critical/standard/experimental) dita thresholds mínimos de error-budget e gates de promoção.

#### A.4 AI Governance — de capacidade para plataforma

- **Agentic Runtime governado** — multi-step plans com policy-bounded tool invocation, budget por plano, human-in-the-loop para ações com blast radius > threshold, auditoria completa do plano.
- **Prompt/Context Registry versionado** — `PromptAsset` com versionamento, eval set associado (prompts como contratos).
- **AI Evaluation Harness** — dataset + métricas por caso de uso, permite trocar modelo com confiança. Ver [ADR-009](./adr/009-ai-evaluation-harness.md).
- **Model Cost Attribution** — cruzar `ExternalAi` com `CostIntelligence` por serviço/equipa/tenant/caso de uso.
- **PII/Secret-aware redaction** — estender `DefaultGuardrailCatalog` para classificar e **mascarar** (não só detectar) antes do envio a modelos externos.

#### A.5 Operational Intelligence — correlação real

- **ML-based Incident↔Change Correlation** — concretizar §8.4 com feature engineering sobre timestamps/services/contracts/ownership; modelo treinado localmente por tenant via stack AI interna.
- **Runbook Execution real** — `RunbookStep.Executable` com conectores a `Integrations`/`Automation`; sugestão-confirmação-execução-auditoria com IA governada no loop.
- **Mitigation Recommendation por similarity search** — indexar incidentes resolvidos em PostgreSQL FTS e sugerir mitigações com citação do incidente de referência.

#### A.6 FinOps contextual — fechar o loop

- **FOCUS spec adoption** — adotar a FinOps Open Cost & Usage Specification como formato canónico interno (já existe `ICloudBillingProvider`).
- **Waste signals por serviço** — idle connections, over-provisioning (via `RuntimeIntelligence`), low-traffic endpoints (via `ContractConsumerInventory`), ações concretas recomendadas por serviço.
- **Cost-aware Change Gate** — bloquear/alertar promoções cujo impacto estimado de custo excede budget do serviço.

---

### Wave B — Reach (expansão de superfície)

#### B.1 Novos tipos de contrato (além dos já roadmapped em §1)

- **AsyncAPI 3.x first-class** — validar paridade total com OpenAPI support.
- **OpenFeature contracts** para feature flags (`Configuration` já tem flags; falta expor como contrato consumível).
- **Terraform module / Helm chart contracts** — governança de plataforma interna.

#### B.2 IDE e Developer Experience (complementa §2)

- **CLI `nexone`** first-class: `nexone contract diff`, `nexone service describe`, `nexone change status <sha>`.
- **GitHub Action / GitLab Template oficial** para *change confidence gate* em PRs.
- **Platform API tokens com scopes fine-grained** alinhados com a autorização do backend.

#### B.3 Ingestão real e ecosistema

- **Kafka/RabbitMQ/Service Bus consumers reais** — mantém o que está em §3.2/3.3.
- **OpenTelemetry Collector recipe oficial** — YAML exporter pronto para qualquer stack OTel alimentar NexTraceOne em minutos.
- **Backstage bridge bidirectional** — export além do `ImportFromBackstage` existente, posicionando NexTraceOne como camada de governance superior.
- **ServiceNow / Jira Change bridge** — importar CRs externos como `Change` entities governadas.

#### B.4 Knowledge Hub — de documentos para grafo vivo

- **Runbook generation from incidents resolvidos** — feedback loop auditado para runbook proposto.
- **Knowledge Freshness Score** por documento (última revisão, ligações ativas, incidentes recentes contraditórios).
- **Semantic search cross-module** sobre Knowledge + Contracts + Incidents + Changes — palette de comando do capítulo 7.6.

---

### Wave C — Assurance (endurecer confiança em produção)

#### C.1 Supply-chain security

- **SLSA Level 3 evidence capture** para releases (proveniência, attestations, SBOM ligadas à `ReleaseIdentity`).
- **Signed artifact verification gate** como Promotion Gate — encaixa em GAP-SEC-03 (§5.1).
- **Dependency vulnerability ingestion** (GHSA, NVD) ligada a `ServiceAsset` e visível em `ServiceMaturity`.

#### C.2 Resilience & compliance evolutivos

- **DORA / NIS2 compliance pack** — alto valor enterprise 2026 para clientes EU financeiros.
- **Access Review escalation automática** via Notifications quando review atrasa.
- **Evidence Pack export assinado** (PDF + JSON + signature) para auditores externos.
- **Multi-region read-replicas** para `AuditDbContext` — audit trail append-only beneficia de geo-redundância.

#### C.3 Observability evolution

- **ClickHouse provider real** (já em §11.2) — acrescentar critérios objetivos de quando preferir ClickHouse vs. Elasticsearch (volume, perfil de query, retenção).
- **eBPF-based runtime signal** — alternativa ao CLR profiler para workloads Linux não-.NET.
- **Continuous Profiling** (pprof / dotnet-trace ingest) contextualizado por serviço — diferencial face a Dynatrace/Datadog em on-prem.

#### C.4 Operação self-hosted e deploy

- **Helm chart oficial + K8s Operator** (§11.1) — acrescentar **HA reference architecture** para >10 tenants e >1000 serviços.
- **Upgrade path automatizado** entre versões — tooling de rollout seguro + rollback para migrações EF Core.
- **Air-gapped install mode** com AI model bundle interno — mercado enterprise defesa/finance.

---

### Wave D — Frontier (apostas estratégicas)

#### D.1 Digital Twin Operacional

Culminação natural do pilar Source of Truth — representação navegável e consultável da "forma atual" do sistema usando Catalog + Contracts + RuntimeIntelligence + ChangeGovernance + CostIntelligence:

- **What-if de mudanças** ("se eu alterar este contrato, quem parte?").
- **Simulação de failure** ("se este serviço cair, qual o impacto?").
- **Navegação temporal** ("como estava isto antes da release X?").

#### D.2 Cross-tenant Benchmarks anonimizados (opt-in)

Benchmarks agregados de DORA, maturity, cost-per-request — valor para Exec/CTO persona. Requer governança forte de privacidade (LGPD/GDPR) e consentimento explícito por tenant.

#### D.3 No-code Policy Studio

Editor visual de políticas (compliance, promotion gates, access) para Platform Admin. OPA/Rego como backend, UI amigável reduzindo dependência de alterar configuração complexa.

#### D.4 Agent-to-Agent protocol

**Agent-facing API** governada (catálogo, contratos, change status, incident status) com autenticação e auditoria específicas de agente. Prepara o produto para ambientes corporativos com múltiplos agentes autónomos.

---

### Priorização recomendada das Waves

Respeita a "Ordem recomendada de priorização do produto" (capítulo 26 das Copilot Instructions):

1. **Wave A completo** — Dentro de A, começar por **A.1 Change Intelligence preditivo** + **A.2 Data Contracts + Consumer Inventory** + **A.4 AI Evaluation Harness** (maior alavanca × risco).
2. **Wave B.2 (IDE/CLI)** + **Wave B.3 (Kafka real + OTel recipe)** — fecham itens já roadmapped e destravam adoção real.
3. **Wave C.1 (supply-chain)** + **Wave C.2 (DORA pack)** — destrancam clientes enterprise regulados.
4. **Wave C.4 (K8s operator + air-gapped)** — destranca escala e mercado defesa/finance.
5. **Wave B.1 novos contratos** (AsyncAPI 3, OpenFeature, Terraform/Helm) cruzados com A.2.
6. **Wave D** — apenas após A/B/C maduros; Digital Twin (D.1) primeiro, por ser o mais defensável.

### Riscos e recomendações transversais

- **Feature sprawl** — resistir à tentação de criar módulos novos; preferir aprofundar existentes (Wave A > Wave B).
- **Foco em Source of Truth** — testar cada feature contra "isto torna o NexTraceOne mais autoritativo ou só mais um dashboard?" (capítulo 4).
- **AI governada como diferencial** — toda evolução de IA passa pelo checklist do capítulo 39 das Copilot Instructions.
- **Observabilidade como meio** — ligar sempre a serviço/contrato/mudança/incidente, nunca dashboards soltos (capítulo 5.4).
- **Configuração por DB** — cada capacidade configurável passa por `ConfigurationDefinitionSeeder`. Nunca `appsettings` para parâmetros de produto.
- **i18n obrigatório** em cada tela nova (pt-PT, pt-BR, en, es).
- **Bounded contexts respeitados** — não resolver gap de domínio apenas no frontend (capítulo 21.4).

---

 ROADMAP.md, EVOLUTION-ROADMAP-2026-2027.md, CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md, SERVICE-CREATION-STUDIO-PLAN.md
2. **Documentos de referência mantidos:** PRODUCT-VISION.md, ARCHITECTURE-OVERVIEW.md, docs/adr/, docs/legacy/, docs/security/, docs/deployment/, docs/runbooks/, docs/observability/, docs/user-guide/
3. **Licensing module** foi removido da solução e não consta neste roadmap
4. **Convites in-app** foram removidos por decisão de produto — onboarding é SSO-first. Ver `docs/HONEST-GAPS.md` (OOS-02).
5. **~98% do produto está implementado** — este roadmap cobre os ~2% restantes + evolução futura. A lista consolidada de gaps abertos está em [HONEST-GAPS.md](./HONEST-GAPS.md).
6. **Customização da plataforma:** Plano detalhado em [PLATFORM-CUSTOMIZATION-EVOLUTION.md](./PLATFORM-CUSTOMIZATION-EVOLUTION.md)
7. **Waves pós-v1.0.0:** Secção 15 consolida Waves A/B/C/D com ADRs associados (ADR-007/008/009).
