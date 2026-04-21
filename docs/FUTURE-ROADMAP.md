# NexTraceOne — Future Roadmap

> **Data:** Abril 2026  
> **Estado atual:** ~98% implementado — todos os módulos core estão READY  
> **Waves concluídas:** A → T (52 features analytics/governance implementadas e testadas)  
> **Waves planeadas:** U → AI (45 features novas documentadas, aguardam implementação)  
> **Wave AA (frontend):** 📘 plano detalhado em [`V3-EVOLUTION-FRONTEND-DASHBOARDS.md`](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md) — 12 waves (V3.1→V3.12) cobrindo Dashboard Intelligence, Frontend Uplift, Collaboration, Marketplace/Plugins, Mobile on-call, Persona Suites, Source-of-Truth Centers e Contract Studio/AI Agents/IDE/Admin consoles  
> **Waves AB–AE (backend avançado):** 4 novas waves planeadas — Knowledge Graph & Semantic Relations, Self-Service & Platform Adoption Intelligence, Zero Trust & Security Posture Analytics, Contract Testing & API Backward Compatibility  
> **Waves AF–AI (backend avançado II):** 4 novas waves planeadas — Service Lifecycle Governance, FinOps Advanced Attribution, Event-Driven Architecture Governance, Predictive Intelligence & Forecasting  
> **Referência:** [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md)

---

## Sumário

Este documento consolida **todas as funcionalidades planeadas** para waves futuros do NexTraceOne. Todas as funcionalidades listadas aqui são **evolução futura** — não são gaps da implementação atual.

O NexTraceOne está operacional com 12 módulos backend, 130+ páginas frontend, 99+ endpoints, 296+ entidades de domínio, 154+ migrações e 2000+ testes.

---

## 1. Novos Tipos de Contrato

### 1.1 GraphQL Contracts ✅ IMPLEMENTADO (Wave G.3 — Abril 2026)
- **Estado:** ✅ `GraphQlSchemaSnapshot` entity + `AnalyzeGraphQlSchema` (Command) + `DetectGraphQlBreakingChanges` (Query) + `GetGraphQlSchemaHistory` (Query). Parsing SDL por keywords sem dependências externas. Breaking change detection: tipos/fields/operations removidos = breaking; adicionados = non-breaking. Migration `20260421160000_G3_AddGraphQlSchemaSnapshots`. 3 config keys (sort 10210–10230). i18n `graphqlSchema.*` em 4 locales. ~15 testes.
- **Futuro:** Visual builder interativo de schemas SDL no Contract Studio — planeado para Wave X.
- **Referência:** GAP-CTR-01, Wave G.3

### 1.2 Protobuf / gRPC Contracts ✅ IMPLEMENTADO (Wave H.1 — Abril 2026)
- **Estado:** ✅ `ProtobufSchemaSnapshot` entity + `AnalyzeProtobufSchema` (Command) + `DetectProtobufBreakingChanges` (Query) + `GetProtobufSchemaHistory` (Query). Parsing `.proto` por keywords (message/enum/service/rpc/syntax). Breaking change detection: messages/services/fields/RPCs removidos = breaking. Migration `20260421170000_H1_AddProtobufSchemaSnapshots`. 3 config keys (sort 10240–10260). i18n `protobufSchema.*` em 4 locales. ~18 testes.
- **Futuro:** Visual builder de `.proto` files e campo de comparação de wire compatibility — planeado para Wave X.
- **Referência:** GAP-CTR-02, Wave H.1

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

### 8.4 ML-based Incident Correlation ✅ IMPLEMENTADO (Wave A.5 — Abril 2026)
- **Estado:** ✅ `ScoreCorrelationFeatureSet` query com feature engineering sobre timestamps/services/ownership. Sub-scores: TemporalProximity + ServiceMatch + OwnershipAlignment + WeightedTotal com labels High/Medium/Low. `ICorrelationFeatureReader` abstraction + `NullCorrelationFeatureReader`. 3 config keys ponderáveis (sort 5200–5220). Wave A.5 completo.
- **Futuro:** Enriquecimento com embedding de logs e traces OTel para correlação semântica — planeado para Wave Y.

---

## 9. Compliance as Code (Evolução)

### 9.1 Advanced Compliance Packs ✅ PARCIALMENTE IMPLEMENTADO (Waves G–L — Abril 2026)
- **Estado:** ✅ 8 standards implementados como queries independentes com scoring contextual:
  - **SOC 2 Type II** — Wave G.1: 5 controlos (CC6/CC7/CC8/CC9/A1)
  - **ISO/IEC 27001:2022** — Wave G.2: 5 controlos Annex A (A.8.8/A.8.32/A.5.26/A.5.29/A.8.9)
  - **PCI-DSS v4.0** — Wave H.2: 5 requisitos (Req 1-2/6/10/11/12)
  - **HIPAA Security Rule** — Wave I.1: 5 controlos (§164.312 a/b/c/d/e)
  - **GDPR** — Wave J.1: 5 controlos (Art. 5/13/17/25/33)
  - **FedRAMP Moderate** — Wave L.2: 5 controlos NIST SP 800-53 (AC-2/AU-2/CM-6/IR-4/SI-2)
  - **NIS2** — Wave D.1.b: 5 controlos RCM
  - **CMMC 2.0 Level 2** — Wave K.2: 5 práticas (AC.1.001/IA.1.076/AU.2.041/IR.2.092/RM.2.141)
- **Futuro:** `GetComplianceCoverageMatrixReport` (visão unificada multi-standard) — planeado para Wave U.1. Controlos `NotAssessed` que dependem de fontes IAM externas — dependem de integração LDAP/AD avançada.

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
| ✅ **DONE** | GraphQL Contracts (1.1) | Análise e breaking change detection de schemas SDL |
| ✅ **DONE** | Protobuf/gRPC Contracts (1.2) | Análise e breaking change detection de schemas `.proto` |
| ✅ **DONE** | SAML (4.1) | Enterprise SSO com mock IdP |
| ✅ **DONE** | Compliance Packs SOC2/ISO27001/PCI-DSS/HIPAA/GDPR/FedRAMP/NIS2/CMMC (9.1) | 8 standards enterprise implementados |
| **Alta** | EF Designer Files (13.1) | Completar tooling de migrações (requer PostgreSQL local) |
| **Alta** | Waves S–W (Analytics Avançados) | 15 novas features planeadas — ver Secção 15 |
| **Média** | IDE Extensions (2.x) | Developer experience inline no editor |
| **Média** | Kafka Integration (3.2) | Event streaming real com consumer |
| **Média** | Wave X — Frontend Dashboards Intelligence | Persona-aware views e executive dashboards |
| **Média** | Wave Y — AI Governance Deep Dive | Agentic runtime, model routing, PII grounding |
| **Baixa** | SDK Externo (3.4) | Automação e integração programática |
| **Baixa** | Contract Sandbox (7.1) | Testing avançado com mock servers |
| **Baixa** | Wave Z — Integration Ecosystem | Kafka consumer real, ClickHouse, SDK |
| **Roadmap** | Legacy/Mainframe (12.x) | Expansão para core systems IBM Z / COBOL |
| **Roadmap** | Kubernetes (11.1) | Helm charts, HPA, service mesh |

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
- **Change Confidence Score 2.0** ✅ — decomposição explicável entregue: agregado `ChangeConfidenceBreakdown` + VO `ChangeConfidenceSubScore` com os 7 sub-scores (`TestCoverage`, `ContractStability`, `HistoricalRegression`, `BlastSurface`, `DependencyHealth`, `CanarySignal`, `PreProdDelta`), citações auditáveis (`citation://…`), `ConfidenceDataQuality` por sub-score e `SimulatedNote` quando dados reais indisponíveis. Feature `ComputeChangeConfidenceBreakdown` + queries `GetChangeConfidenceBreakdown`/`GetChangeConfidenceTimeline`; migration `20260420120000_AddChangeConfidenceBreakdown` (tabelas `chg_confidence_breakdowns` + `chg_confidence_sub_scores`); 7 config keys de pesos (`change.confidence.weights.*`) + `change.confidence.minConfidenceForPromotion` + `change.confidence.historicalWindow.days`; endpoints em `ChangeConfidenceEndpoints.cs`; testes `ChangeConfidenceBreakdownTests.cs`. Ver [ADR-008](./adr/008-change-confidence-score-v2.md).
- **Promotion Readiness Delta** ⚠️ *Parcialmente entregue (backend slice)* — superfície read-only `IRuntimeComparisonReader` owned por ChangeGovernance (`ChangeGovernance.Application/ChangeIntelligence/Abstractions/IRuntimeComparisonReader.cs`) + `NullRuntimeComparisonReader` honest-null default + feature VSA `GetPromotionReadinessDelta` (Query/Validator/Handler/Response) com classificação defensiva `Ready/Review/Blocked/Unknown` baseada em deltas de erro, latência e incidentes + endpoint `GET /api/v1/changes/promotion-readiness-delta` com permissão `change-intelligence:read` + 8 testes unitários (`GetPromotionReadinessDeltaTests.cs`). **Pendências explícitas:** (a) bridge real p/ OperationalIntelligence a registar na composition root (ApiHost) substituindo o default — a primitiva `CompareEnvironments` de OI já existe; (b) integração nos Promotion Gates existentes como gate não-bloqueante por defeito, controlado por config; (c) UI no ReleaseTrain (cards de delta + badge de readiness + sinalização de `SimulatedNote`); (d) i18n em 4 locales para a UI. Sem a bridge, o endpoint devolve snapshot simulado com `SimulatedNote` preenchido para que a UX sinalize o estado — preservando a regra "honest gaps" e sem falsos positivos.
- **TTD / TTR KPIs por equipa** — cruzar `CostIntelligence` × incidentes, já existem hooks; falta produto de UX por persona.

#### A.2 Contract Governance — contratos como dados vivos

- **Data Contracts** — novo `ContractType.DataContract` para tabelas/vistas/streams analíticos (owner, SLA de frescura, schema, PII classification). Ver [ADR-007](./adr/007-data-contracts.md).
- **Contract-to-Consumer Tracking real via OTel** — persistir `ContractConsumerInventory` (quem chamou, com que versão, em que ambiente, com que frequência) derivado de traces já ingeridos. Habilita deprecation governance útil.
- **Contract Linting Marketplace** — pacotes Spectral oficiais distribuídos via `ConfigurationDefinitionSeeder` (enterprise, security, accessibility, internal-platform), ativáveis por tenant.
- **Breaking Change Proposal Workflow** — workflow formal antes de publicar v2 com quebras: consulta consumidores, janela de migração, `DeprecationPlan` automático.

#### A.3 Service Catalog — ownership vivo e scorecards ✅

- **Ownership Drift Detection** ✅ — `DetectOwnershipDrift` query por serviço + `GetOwnershipDriftReport` relatório por tenant, com sinais de drift (last review, on-call, owner, contact channel), threshold configurável via `catalog.ownershipDrift.threshold.days` e `ReviewServiceOwnership` command para repor o timer.
- **ServiceTierType** ✅ — enum `Critical/Standard/Experimental` em domínio, propriedade `Tier` em `ServiceAsset`, `SetTier()` e `RecordOwnershipReview()`, migration `20260420150000_C_ServiceTierAndOwnershipReview`.
- **Tier-based SLO enforcement** ✅ — `GetServiceTierPolicy` query retorna thresholds mínimos de SLO, maturidade, on-call e runbook por tier; conformância verificada em tempo real; 6 chaves de config no `ConfigurationDefinitionSeeder` (sort 5100-5150).
- **i18n** ✅ — `serviceTier.*` + `ownershipDrift.*` em 4 locales (en/es/pt-BR/pt-PT).
- **5 novos endpoints** ✅ — `PUT /services/{id}/tier`, `GET /services/{id}/tier-policy`, `GET /services/{id}/ownership-drift`, `POST /services/{id}/ownership-review`, `GET /ownership/drift-report`.
- **15 testes unitários** ✅ — `ServiceCatalogV2Tests.cs`. Total Catalog: 1657/1657.
- **Service Maturity v2** — roadmapped para Wave A.5; a base tier/drift criada aqui alimenta as dimensões de maturidade futuras.

#### A.4 AI Governance — de capacidade para plataforma

- **Agentic Runtime governado** — multi-step plans com policy-bounded tool invocation, budget por plano, human-in-the-loop para ações com blast radius > threshold, auditoria completa do plano.
- **Prompt/Context Registry versionado** — `PromptAsset` com versionamento, eval set associado (prompts como contratos).
- **AI Evaluation Harness** — dataset + métricas por caso de uso, permite trocar modelo com confiança. Ver [ADR-009](./adr/009-ai-evaluation-harness.md).
- **Model Cost Attribution** — cruzar `ExternalAi` com `CostIntelligence` por serviço/equipa/tenant/caso de uso.
- **PII/Secret-aware redaction** ✅ — estendido `DefaultGuardrailCatalog` para **mascarar** (não só detectar): `pii-email-detection` e `pii-phone-detection` actualizados para `GuardrailAction.Sanitize`; adicionados `pii-credit-card-redaction` (PAN 13–19 dígitos, Critical), `pii-national-id-redaction` (SSN/NIF/tax-id, High) e `secret-bearer-token-redaction` (bearer + JWT, Critical), todos com `Action = Sanitize`. Catálogo oficial cresce de 8 → 11 guardrails. 2 novos testes de domínio (`Catalog_Has_Expected_Count`, `Contains_Pii_Redaction_Guardrails_With_Sanitize_Action`).

#### A.5 Operational Intelligence — correlação real ✅

- **ML-based Incident↔Change Correlation** ✅ — `ScoreCorrelationFeatureSet` query com feature engineering sobre timestamps/services/ownership; sub-scores TemporalProximity + ServiceMatch + OwnershipAlignment + WeightedTotal com labels High/Medium/Low. `ICorrelationFeatureReader` abstraction + `NullCorrelationFeatureReader`. 3 config keys ponderáveis (sort 5200-5220).
- **Runbook Execution real** ✅ — `RunbookStepExecution` domain entity (Id fortemente tipado, `RunbookStepExecutionStatus` enum, `MarkSucceeded/MarkFailed`), `IRunbookExecutionRepository`, `EfRunbookExecutionRepository`, migration `20260420160000_OI_AddRunbookStepExecution` (table `ops_inc_runbook_step_executions`), `ExecuteRunbookStep` command feature, endpoint `POST /incidents/runbooks/{id}/steps/{key}/execute`.
- **Mitigation Recommendation por similarity search** ✅ — `GetMitigationRecommendationsBySimilarity` query reutiliza scoring de `FindSimilarIncidents` sobre incidentes resolvidos e retorna sugestões com citação do incidente de referência.
- **3 novos endpoints** ✅ — `GET /incidents/{id}/changes/{changeId}/feature-score`, `GET /incidents/{id}/mitigation/similar-recommendations`, `POST /incidents/runbooks/{id}/steps/{key}/execute`.
- **6 config keys** ✅ (sort 5200-5250): temporalWeight, serviceWeight, ownershipWeight, maxConcurrentExecutions, lookbackDays, minScore.
- **i18n** ✅ — `correlationFeatures.*` + `runbookExecution.*` + `mitigationSimilarity.*` em 4 locales.
- **24 testes unitários** ✅ — `OperationalIntelligenceA5Tests.cs`. OI: 956/956.

#### A.6 FinOps contextual — fechar o loop ✅

- **FOCUS spec adoption** ✅ — `GetFocusExport` query exporta `CostRecord` no formato canónico FinOps FOCUS (BilledCost, BillingCurrency, ChargePeriodStart/End, ServiceName, ServiceCategory, Provider, ResourceId, Tags). Endpoint: `GET /api/v1/cost/focus-export` com paginação. Config key `finops.focus.pageSize` (sort 5340).
- **Waste signals por serviço** ✅ — `WasteSignal` domain entity + `WasteSignalType` enum (`IdleResource/OverProvisioned/LowTrafficEndpoint/UnusedReservation`). `DetectWasteSignals` command: heurística de mediana (idle < threshold%, overprovision ≥ multiplier×median) sobre `CostRecord`. `GetWasteReport` query: agrega sinais por tipo com total estimado. EF config + `WasteSignalRepository` + migration `20260420170000_OI_AddWasteSignals` (table `ops_cost_waste_signals`). 4 endpoints. 6 config keys (sort 5300-5350). i18n `finOps.*` + `wasteSignal.*` + `costGate.*` em 4 locales.
- **Cost-aware Change Gate** ✅ — `EvaluateCostAwareChangeGate` query consulta `ServiceCostProfile` (budget + currentCost + alertThreshold) e retorna `GateResult` (NotConfigured/Passed/Warning/Blocked) com `ShouldBlock` controlado por `finops.changeGate.blockOnBudgetExceed`. Endpoint: `GET /api/v1/cost/change-gate`.
- **21 testes unitários** ✅ — `FinOpsA6Tests.cs`. OI: 977/977.

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
- **OpenTelemetry Collector recipe oficial** ✅ — YAML exporter pronto para qualquer stack OTel alimentar NexTraceOne em minutos (`docs/otel-collector-recipe.yaml` + `docs/OTEL-INTEGRATION-GUIDE.md`).
- **Backstage bridge bidirectional** ✅ — `ExportToBackstage` query no módulo Catalog (endpoint `GET /api/v1/catalog/services/export/backstage`), mapeando ServiceAssets para entidades Backstage Component com anotações NexTraceOne, namespace, lifecycle e owner configuráveis. Complementa o `ImportFromBackstage` existente. Config keys `integrations.backstage.instanceUrl` e `integrations.backstage.exportEnabled` (sort 5450–5460).
- **ServiceNow / Jira Change bridge** ✅ — `ExternalChangeRequest` domain entity com `ExternalChangeRequestStatus` enum, `IExternalChangeRequestRepository`, `ImportExternalChangeRequest` command (idempotente por chave natural ExternalSystem+ExternalId), `EfExternalChangeRequestRepository`, migration `20260420190000_CG_AddExternalChangeRequest` (tabela `cg_external_change_requests`). Endpoints `POST/GET /api/v1/changes/external-change-requests`. Config keys `integrations.externalChange.autoLinkEnabled` e `integrations.externalChange.allowedSystems` (sort 5470–5480). 22 testes unitários.

#### B.4 Knowledge Hub — de documentos para grafo vivo ✅

- **Knowledge Freshness Score** ✅ — `ComputeFreshnessScore()` em `KnowledgeDocument` (0–100 clamped): penalização por nunca revisto, antiguidade > 90/180/365 dias, sem links activos, incidentes recentes; bónus para docs bem conectados. Novas propriedades: `LastReviewedAt`, `ReviewedBy`, `ActiveLinkCount`, `RecentIncidentCount`. Métodos: `MarkReviewed()`, `UpdateLinkCount()`, `UpdateRecentIncidentCount()`. `ScoreDocumentFreshness` query (label: Stale/Aging/Fresh/Excellent). `GetFreshnessReport` query (AverageScore, StaleCount, FreshCount).
- **Runbook generation from incidents resolvidos** ✅ — `ProposedRunbook` domain entity (`ProposedRunbookStatus`: Draft/UnderReview/Approved/Rejected) + `IProposedRunbookRepository`. `ProposeRunbookFromIncident` command: bounded-context-safe (aceita dados do incidente inline), gera steps de MitigationActions, auto-approve via config `knowledge.runbook.autoApprove`. Migration `20260420180000_KNW_AddFreshnessAndProposedRunbook` (tabela `knw_proposed_runbooks` + colunas freshness em `knw_knowledge_documents`).
- **Semantic search cross-module** ✅ — `SearchAcrossModules` query: pesquisa sobre KnowledgeDocuments + OperationalNotes + ProposedRunbooks com relevance scoring (1.0 title exact → 0.9 title contains → 0.7 content → 0.5 tags). Endpoint `GET /api/v1/knowledge/search`.
- **4 novos endpoints**, 5 config keys (sort 5400–5440), i18n `knowledgeFreshness.*` + `proposedRunbook.*` + `knowledgeSearch.*` em 4 locales, **20 testes unitários**. Knowledge: 123/123.

---

### Wave C — Assurance (endurecer confiança em produção)

#### C.1 Supply-chain security ✅ **(Implementado — 2026-04-21)**

- ✅ **Dependency vulnerability ingestion** (GHSA, NVD) — `VulnerabilityAdvisoryRecord` entity persistável no módulo Catalog.DependencyGovernance. Ingestão idempotente por `(ServiceId, AdvisoryId)` via `IngestAdvisoryReport` command. 
- ✅ **Vulnerability Promotion Gate** — `EvaluateVulnerabilityPromotionGate` bloqueia promoção quando critical/high advisories excedem thresholds configuráveis (`security.vulnerability.gate.max_critical`, `security.vulnerability.gate.max_high`).
- ✅ **Vulnerability Gate Summary** — `GetServiceVulnerabilityGateSummary` agrega contagens por severidade (Critical/High/Medium/Low) com CVSS score máximo, para uso em dashboards e gates.
- ✅ Migration `20260421070000_C1_AddVulnerabilityAdvisoryRecords` + 5 config keys (sort 5900–5940) + i18n 4 locales + 17 testes.
- ✅ **SLSA Level 3 evidence capture** — proveniência, attestations e SBOM no `ReleaseIdentity` (Wave D backlog).
- ✅ **Signed artifact gate** — verificação de assinatura de artefacto como Promotion Gate dedicado (Wave D backlog).

#### C.2 Resilience & compliance evolutivos ✅ **(Parcialmente implementado — 2026-04-21)**

- ✅ **DORA metrics** — `GetDoraMetrics` (Deployment Frequency, Lead Time, Change Failure Rate, MTTR) já implementado em wave anterior com classificação Elite/High/Medium/Low.
- ✅ **Evidence Pack signed export** — `SignEvidencePack` aplica HMAC-SHA256 sobre manifesto canónico. `VerifyEvidencePackIntegrity` valida assinatura para auditores externos. Chave de assinatura configurável (`security.evidence_pack.signing_key` — `SensitiveOperational`). Migration `20260421080000_C2_AddEvidencePackSignature`.
- ✅ **Access Review escalation automática** — `EscalateOverdueAccessReviews` identifica campanhas próximas do prazo e envia notificações via `INotificationModule`. `ListOpenApproachingDeadlineAsync` adicionado ao repositório. Config keys sort 5970–5980.
- ✅ 4 config keys + i18n 4 locales + 18 testes (10 CG + 8 IA).
- 🔲 ~~**NIS2 compliance report**~~ ✅ IMPLEMENTADO — `GetNis2ComplianceReport` query em `ChangeGovernance.Application/Compliance/Features/GetNis2ComplianceReport`. Avalia 5 controlos NIS2 (RCM-1 a RCM-5): gestão de risco, integridade de evidência (Evidence Pack assinado via Wave C.2), vulnerabilidades, controlo de acesso, rastreabilidade de releases. Estado por controlo: `Nis2ControlStatus` (NotAssessed/Compliant/PartiallyCompliant/NonCompliant). Enum em `ChangeGovernance.Domain/Compliance/Enums/`. Config: `compliance.nis2.enabled` (sort 9990) + `compliance.nis2.report.period_days` (sort 10000). i18n `nis2Compliance.*` em 4 locales. 12 testes em `GetNis2ComplianceReportTests.cs`. CG: 572/572.
- 🔲 **Multi-region read-replicas** para `AuditDbContext` — decisão de infra, sem impacto de código imediato.

#### C.3 Observability evolution

- 🔲 **ClickHouse provider real** — `ClickHouseAnalyticsWriter` já existe como adapter. Critérios objectivos ClickHouse vs. Elasticsearch: ClickHouse preferível para >100M events/day, queries de agregação OLAP pesadas, retenção longa e baixo custo storage; Elasticsearch preferível para full-text search, correlação de logs e queries ad-hoc em <10M eventos.
- 🔲 **eBPF-based runtime signal** — alternativa ao CLR profiler para workloads Linux não-.NET (Wave D backlog).
- ✅ **Continuous Profiling** (pprof / dotnet-trace ingest) contextualizado por serviço — diferencial face a Dynatrace/Datadog em on-prem (Wave D backlog).

#### C.4 Operação self-hosted e deploy

- 🔲 **Helm chart oficial + K8s Operator** — HA reference architecture para >10 tenants e >1000 serviços.
- ✅ **Upgrade path automatizado** — tooling de rollout seguro + rollback para migrações EF Core.
- 🔲 **Air-gapped install mode** com AI model bundle interno — mercado enterprise defesa/finance.

---

### Wave D — Frontier (apostas estratégicas)

#### D.1 Digital Twin Operacional ✅ PARCIALMENTE IMPLEMENTADO (Abril 2026)

Culminação natural do pilar Source of Truth — representação navegável e consultável da "forma atual" do sistema usando Catalog + Contracts + RuntimeIntelligence + ChangeGovernance + CostIntelligence:

- **What-if de mudanças** ✅ — `SimulateContractChangeImpact` query em `Catalog.Application/Contracts/Features/SimulateContractChangeImpact`. Recebe `ApiAssetId` + `WhatIfChangeType` (Additive/NonBreaking/Breaking/Deprecation) e classifica cada `ConsumerExpectation` activa em `WhatIfImpactLevel` (None/Low/Medium/High/Critical) com razão e recomendação. `TotalConsumers`, `ImpactedConsumers`, `OverallRisk` no relatório. Enums `WhatIfChangeType` + `WhatIfImpactLevel` em `Catalog.Domain.Contracts.Enums`. 14 testes em `DigitalTwinD1Tests.cs`. Catalog: 1701/1701.
- **Navegação temporal** ✅ — `GetLatestTopologySnapshot` query retorna o snapshot mais recente do grafo com `NodeCount`, `EdgeCount`, `CapturedAt`, `NodesJson`, `EdgesJson`. Usa `IGraphSnapshotRepository.GetLatestAsync()`.
- **Simulação de failure** ✅ IMPLEMENTADO — `SimulateServiceFailureImpact` query em `Catalog.Application/Graph/Features/SimulateServiceFailureImpact`. Recebe `ServiceAssetId` + `MaxDepth` (1–10, default 3), encontra todas as APIs expostas pelo serviço, propaga impacto transitivo pelos `ConsumerRelationship`, calcula `CascadeRisk` (critical/high/medium/low) com base no `ServiceTierType` + contagem de impactados. Retorna `DirectImpactCount`, `TransitiveImpactCount`, `TotalImpacted`, `ImpactedNodes`. Config `digital_twin.failure_sim.max_depth` (sort 9980). i18n `digitalTwin.failureSim.*` em 4 locales. 12 testes em `FailureSimD1bTests.cs`. Catalog: 1713/1713.
- **Config**: 3 chaves `digital_twin.*` (sort 9910–9920) + i18n `digitalTwin.*` em 4 locales.

#### D.2 Cross-tenant Benchmarks anonimizados (opt-in) ✅ IMPLEMENTADO (Abril 2026)

Benchmarks agregados de DORA, maturity, cost-per-request — valor para Exec/CTO persona. Governança forte de privacidade (LGPD/GDPR) com consentimento explícito por tenant.

- **`TenantBenchmarkConsent`** entity ✅ — consentimento opt-in por tenant com `BenchmarkConsentStatus` (NotRequested/Pending/Granted/Revoked), `LgpdLawfulBasis`, `ConsentedByUserId`, `ConsentedAt`, `RevokedAt`. Factory `RequestConsent`, métodos `Grant(userId, now)` e `Revoke(userId, now)`.
- **`BenchmarkSnapshotRecord`** entity ✅ — métricas DORA (`DeploymentFrequencyPerWeek`, `LeadTimeForChangesHours`, `ChangeFailureRatePercent`, `MeanTimeToRestoreHours`), `MaturityScore` (0-100), `CostPerRequestUsd` (FinOps), `IsAnonymizedForBenchmarks`. Factory `Record(...)` + método `MarkAsAnonymized()`.
- **3 features** ✅: `RecordBenchmarkConsent` (command — Request/Grant/Revoke), `SubmitBenchmarkSnapshot` (command — requer Granted consent), `GetCrossRankedBenchmark` (query — percentil vs peer set anonimizado).
- **Privacidade obrigatória**: `GetCrossRankedBenchmark` NUNCA retorna dados individuais de outros tenants — apenas agrega e calcula percentis. Peer set mínimo configurável (default 5) para protecção de privacidade.
- **EF Core**: `chg_benchmark_consents` + `chg_benchmark_snapshots` com índices por TenantId, período e IsAnonymizedForBenchmarks (filtered).
- **Migration `20260421120000_CG_AddBenchmarkConsents`** ✅ — tabelas `chg_benchmark_consents` + `chg_benchmark_snapshots`.
- **3 config keys** `benchmark.*` (sort 10060–10075) + i18n `benchmark.*` em 4 locales.
- **15 testes unitários** ✅ — `BenchmarkTests.cs`. CG: 607/607.

#### D.3 No-code Policy Studio ✅ IMPLEMENTADO (Abril 2026)

Editor de políticas no-code para Platform Admin. DSL JSON estruturado puro — sem dependência de OPA/Rego. Engine de avaliação em C# puro com semântica AND e fail-open configurável.

- **`PolicyDefinition`** entity ✅ — `PolicyDefinitionType` (PromotionGate/AccessControl/ComplianceCheck/FreezeWindow/AlertThreshold), `IsEnabled`, `Version` (auto-incrementado em `UpdateRules`), `RulesJson` (array JSON de `{Field, Operator, Value}`), `ActionJson` (`{action, message}`), `AppliesTo`, `EnvironmentFilter`. `PolicyRuleOperator`: Equals/NotEquals/GreaterThan/LessThan/Contains/NotContains/Matches.
- **`PolicyEvaluationResult`** value object ✅ — `Passed`, `Action`, `Message`, `RuleTriggered`.
- **`PolicyDefinition.Evaluate(contextJson)`** ✅ — engine puro em C#, `System.Text.Json`, sem deps externas. ALL rules AND semantics. Fail-open em erros de parse. GreaterThan/LessThan com `decimal.TryParse`.
- **4 features** ✅: `CreatePolicyDefinition`, `UpdatePolicyDefinition`, `EvaluatePolicyDefinition`, `ListPolicyDefinitions` (com filtro por tipo e estado enabled).
- **EF Core**: `iam_policy_definitions` com índices por (TenantId, PolicyType) e IsEnabled (filtered).
- **Migration `20260421130000_IAM_AddPolicyDefinitions`** ✅ — tabela `iam_policy_definitions`.
- **3 config keys** `policy_studio.*` (sort 10080–10100) + i18n `policyStudio.*` em 4 locales.
- **15 testes unitários** ✅ — `PolicyStudioTests.cs`. IA: 550/550.

#### D.4 Agent-to-Agent protocol ✅ IMPLEMENTADO (Abril 2026)

**Agent-facing API** governada (catálogo, contratos, change status, incident status) com autenticação e auditoria específicas de agente. Prepara o produto para ambientes corporativos com múltiplos agentes autónomos.

- **`PlatformApiToken`** aggregate ✅ — entidade em `IdentityAccess.Domain` com `TokenHash` (SHA-256), `TokenPrefix` (8 chars), `PlatformApiTokenScope` (Read/ReadWrite/Admin), `ExpiresAt`, `RevokedAt`, `LastUsedAt`. O valor real do token é apresentado apenas uma vez na criação (não é persistido). 5 features: `CreatePlatformApiToken` (geração segura via `RandomNumberGenerator`), `RevokePlatformApiToken`, `ListPlatformApiTokens`, `RecordAgentQuery`, `GetAgentQueryAuditLog`.
- **`AgentQueryRecord`** entity ✅ — registo de auditoria de cada query de agente com `TokenId`, `QueryType`, `QueryParametersJson`, `ResponseCode`, `DurationMs`, `ExecutedAt`, `ErrorMessage`. Rastreabilidade completa de acções de agentes.
- **`IPlatformApiTokenRepository`** + **`IAgentQueryRepository`** + implementações EF ✅.
- **Migration `20260421090000_IAM_AddPlatformApiTokensAndAgentAudit`** ✅ — tabelas `iam_platform_api_tokens` + `iam_agent_query_records` com índices por tenant, token hash (unique), tokenId, executedAt.
- **4 config keys** `agent.api.*` (sort 9930–9960) + i18n `agentApi.*` em 4 locales.
- **16 testes unitários** ✅ — `AgentApiD4Tests.cs`. IA: 530/530.

---

### Wave E — Operations & Upgrade Intelligence ✅ COMPLETO (Abril 2026)

#### E.1 Continuous Profiling ✅ IMPLEMENTADO (Abril 2026)

Contextualizado por serviço/release/ambiente — diferencial enterprise face a Dynatrace/Datadog em on-prem.

- **`ProfilingSession`** aggregate ✅ — entidade com `ServiceAssetId`, `ServiceName`, `Environment`, `FrameType` (`ProfilingFrameType`: CpuSampling/MemoryAllocation/WallClockTime/ThreadContention), `ProfileDataBase64`, `DurationMs`, `SampleCount`, `CollectedAt`, `MetadataJson`. Método `Attach` para associar a uma release.
- **3 features** ✅: `IngestProfilingSession` (command + persistência), `GetProfilingHistory` (query com filtro ambiente/tipo), `GetProfilingAnalysis` (análise sumária: frame distribution, top functions).
- **Migration `20260421110000_OI_AddProfilingSession`** ✅ — tabela `oi_profiling_sessions`.
- **3 config keys** `profiling.*` (sort 10030–10050) + i18n `profiling.*` em 4 locales.
- **25 testes unitários** ✅ — `ProfilingTests.cs`. OI: 992/992.

#### E.2 Migration Health Report ✅ IMPLEMENTADO (Abril 2026)

Visibilidade de saúde das migrações de base de dados por módulo para Platform Admin.

- **`GetMigrationHealthReport`** query ✅ — retorna estado de migrações pendentes, aplicadas e falhas por módulo (`ModuleHealthDto`), com `IsHealthy`, `MigrationCount`, `PendingCount`, `LastAppliedAt`.
- **3 config keys** `migration.*` (sort 10030–10050) + i18n `migrationHealth.*` em 4 locales.
- **Integrado em `IdentityAccess` module** ✅. IA: 535/535.

---

### Wave F — Release Calendar + Risk Center ✅ COMPLETO (Abril 2026)

#### F.1 Release Calendar ✅ IMPLEMENTADO (Abril 2026)

Calendário de janelas de deployment, congelamento, hotfix e manutenção por tenant/ambiente. Alimenta o `IsChangeWindowOpen` que bloqueia promoções durante freezes activos.

- **`ReleaseCalendarEntry`** aggregate ✅ — entidade em `ChangeGovernance.Domain.ChangeIntelligence.Entities` com `ReleaseWindowType` (Scheduled/Freeze/HotfixAllowed/Maintenance) e `ReleaseWindowStatus` (Active/Closed/Cancelled). Comportamentos: `Register`, `Close`, `Cancel`, `IsActiveAt`. Computed properties `BlocksChanges` e `IsHotfixOnly`. Strongly-typed ID `ReleaseCalendarEntryId`.
- **`IReleaseCalendarRepository`** + implementação EF ✅ — `ListAsync` (filtros por estado, tipo, intervalo), `ListActiveAtAsync` (janelas activas num momento por ambiente).
- **4 features** ✅:
  - `RegisterReleaseWindow` — regista nova janela com validação de período
  - `CloseReleaseWindow` — encerra (Close) ou cancela (Cancel) uma janela
  - `ListReleaseWindows` — lista janelas por tenant/filtros
  - `IsChangeWindowOpen` — resposta semântica: `IsOpen`, `Reason`, `BlockingWindows[]`
- **Migration `20260421140000_CG_AddReleaseCalendar`** ✅ — tabela `chg_release_calendar_entries` com índices por tenant/tipo/estado e tenant/período.
- **3 config keys** `release_calendar.*` (sort 10110–10130) + i18n `releaseCalendar.*` em 4 locales.
- **~20 testes unitários** ✅ — `ReleaseCalendarTests.cs`.

#### F.2 Risk Center ✅ IMPLEMENTADO (Abril 2026)

Perfil de risco agregado por serviço, ranqueado por criticidade, para persona Platform Admin / CTO / Executive. Alimentado por sinais de vulnerabilidade, change failure rate, blast radius e violações de política.

- **`RiskLevel`** enum ✅ — Negligible/Low/Medium/High/Critical (em `ChangeGovernance.Domain.Compliance.Enums`).
- **`RiskSignalType`** enum ✅ — VulnerabilityCritical/HighChangeFailureRate/LargeBlastRadius/PolicyViolation/NoOwner/StaleContract/UnreviewedRelease.
- **`ServiceRiskProfile`** entity ✅ — scores dimensionais normalizados 0–100 com fórmula ponderada: vuln 40% + change_failure 25% + blast_radius 20% + policy 15%. `ActiveSignalsJson` (JSON com signal + reason). Factory `Compute` com clamping e classificação automática de nível.
- **`IServiceRiskProfileRepository`** + implementação EF ✅ — `GetLatestByServiceAsync`, `ListByTenantRankedAsync` (group by service, latest per service, order by score desc).
- **3 features** ✅:
  - `ComputeServiceRiskProfile` — calcula e persiste o perfil de risco
  - `GetServiceRiskProfile` — obtém o perfil mais recente de um serviço
  - `GetRiskCenterReport` — lista serviços ranqueados com distribuição de níveis
- **Migration `20260421150000_CG_AddRiskProfiles`** ✅ — tabela `chg_service_risk_profiles` com índices por tenant/service/computed e tenant/risk_level.
- **3 config keys** `risk_center.*` (sort 10140–10160) + i18n `riskCenter.*` em 4 locales.
- **~15 testes unitários** ✅ — `RiskCenterTests.cs`.

**Totais Wave F:** CG: 642 testes. Configuração: +6 config keys (sort 10110–10160). i18n: +2 secções (4 locales).

---

### Wave G — Compliance Avançada + GraphQL Schema Analysis ✅ COMPLETO (Abril 2026)

Expande a capacidade de conformidade do NexTraceOne com dois relatórios de auditoria de mercado (SOC 2 e ISO/IEC 27001:2022) e adiciona suporte de primeira classe para schemas GraphQL no módulo de Contratos.

#### G.1 SOC 2 Compliance Report ✅ IMPLEMENTADO (Abril 2026)

Relatório de conformidade SOC 2 Type II para auditores externos e clientes enterprise. Avalia 5 controlos com base em dados já existentes no NexTraceOne.

- **`GetSoc2ComplianceReport`** query handler ✅ — cobre controlos CC6 (Logical Access), CC7 (System Operations), CC8 (Change Management), CC9 (Risk Mitigation) e A1 (Availability). CC8 avaliado por presença de releases + Evidence Packs assinados. Controlos CC6 e CC9 marcados como `NotAssessed` até integração com IdentityAccess e Risk Center.
- **Filtro por serviço e período** ✅ — query aceita `Days` (1–365) e `ServiceName` opcional.
- **2 config keys** `compliance.soc2.*` (sort 10170–10180) + i18n `soc2Compliance.*` em 4 locales.
- **~10 testes unitários** ✅ — `GetSoc2ComplianceReportTests.cs`.

#### G.2 ISO 27001 Compliance Report ✅ IMPLEMENTADO (Abril 2026)

Relatório de conformidade ISO/IEC 27001:2022 para auditores e clientes com requisitos de certificação. Cobre 5 controlos Annex A do standard 2022.

- **`GetIso27001ComplianceReport`** query handler ✅ — cobre A.8.8 (Technical Vulnerability Management), A.8.32 (Change Management), A.5.26 (Incident Response), A.5.29 (Business Continuity) e A.8.9 (Configuration Management). A.8.32 avaliado por releases + Evidence Packs assinados.
- **Filtro por serviço e período** ✅ — query aceita `Days` (1–365) e `ServiceName` opcional.
- **2 config keys** `compliance.iso27001.*` (sort 10190–10200) + i18n `iso27001Compliance.*` em 4 locales.
- **~10 testes unitários** ✅ — `GetIso27001ComplianceReportTests.cs`.

#### G.3 GraphQL Schema Analysis ✅ IMPLEMENTADO (Abril 2026)

Suporte de primeira classe para schemas GraphQL SDL no módulo de Contratos. Parsing leve sem dependências externas (zero NuGet libs adicionais), persistência de snapshots e detecção de breaking changes semânticos.

- **`GraphQlSchemaSnapshot`** entity ✅ — armazena schema SDL, tipo/campo/operation counts, JSON estruturado (`TypeNamesJson`, `OperationsJson`, `FieldsByTypeJson`), booleans `HasQueryType/HasMutationType/HasSubscriptionType`. Strongly-typed ID `GraphQlSchemaSnapshotId`.
- **`IGraphQlSchemaSnapshotRepository`** + implementação EF ✅ — `Add`, `GetLatestByApiAssetAsync`, `ListByApiAssetAsync` (paginado), `GetByIdAsync`.
- **3 features VSA** ✅:
  - `AnalyzeGraphQlSchema` (Command) — parsing SDL por keywords, persiste snapshot estruturado. Suporta schemas até 512 KB.
  - `DetectGraphQlBreakingChanges` (Query) — compara dois snapshots: tipos removidos, fields removidos, operations removidas = breaking; tipos/fields/operations adicionados = non-breaking.
  - `GetGraphQlSchemaHistory` (Query) — lista snapshots por ApiAsset paginados do mais recente para o mais antigo.
- **EF Configuration** `GraphQlSchemaSnapshotConfiguration` ✅ — tabela `ctr_graphql_schema_snapshots` com índices `ix_ctr_graphql_snapshots_api_tenant_captured` e `ix_ctr_graphql_snapshots_tenant_captured`.
- **Migration `20260421160000_G3_AddGraphQlSchemaSnapshots`** ✅.
- **3 config keys** `graphql.schema.*` (sort 10210–10230) + i18n `graphqlSchema.*` em 4 locales.
- **~15 testes unitários** ✅ — `GraphQlSchemaAnalysisTests.cs`.

**Totais Wave G:** CG: 662 testes (+20). Catalog: 1726 testes (+13). Configuração: +7 config keys (sort 10170–10230). i18n: +3 secções (4 locales). **WAVE G COMPLETO**.

---

### Wave H — Protobuf Schema Analysis + PCI-DSS + Maturity Score v2 ✅ COMPLETO (Abril 2026)

Expande o suporte de schemas para gRPC/Protobuf, adiciona conformidade PCI-DSS v4.0 e introduz um scorecard de maturidade v2 com pesos conscientes do tier de serviço e postura de vulnerabilidade.

#### H.1 Protobuf Schema Analysis ✅ IMPLEMENTADO (Abril 2026)

Suporte de primeira classe para schemas Protobuf `.proto` no módulo de Contratos. Parsing leve sem dependências externas, persistência de snapshots e detecção de breaking changes semânticos em messages, fields, services e RPCs.

- **`ProtobufSchemaSnapshot`** entity ✅ — armazena schema .proto, message/field/service/RPC counts, JSON estruturado (`MessageNamesJson`, `FieldsByMessageJson`, `RpcsByServiceJson`), campo `Syntax` (proto2/proto3). Strongly-typed ID `ProtobufSchemaSnapshotId`.
- **`IProtobufSchemaSnapshotRepository`** + implementação EF ✅ — `Add`, `GetLatestByApiAssetAsync`, `ListByApiAssetAsync` (paginado), `GetByIdAsync`.
- **3 features VSA** ✅:
  - `AnalyzeProtobufSchema` (Command) — parsing por keywords (message/enum/service/rpc/syntax), persiste snapshot. Suporta até 256 KB.
  - `DetectProtobufBreakingChanges` (Query) — compara dois snapshots: messages/services/fields/RPCs removidos = breaking; adicionados = non-breaking.
  - `GetProtobufSchemaHistory` (Query) — lista snapshots por ApiAsset paginados do mais recente para o mais antigo.
- **EF Configuration** `ProtobufSchemaSnapshotConfiguration` ✅ — tabela `ctr_protobuf_schema_snapshots` com índices adequados.
- **Migration `20260421170000_H1_AddProtobufSchemaSnapshots`** ✅.
- **3 config keys** `protobuf.schema.*` (sort 10240–10260) + i18n `protobufSchema.*` em 4 locales.
- **~18 testes unitários** ✅ — `ProtobufSchemaAnalysisTests.cs`.

#### H.2 PCI-DSS Compliance Report ✅ IMPLEMENTADO (Abril 2026)

Relatório de conformidade PCI-DSS v4.0 para auditores externos e clientes em ambientes de processamento de pagamentos. Avalia 5 requisitos com base em dados já existentes no NexTraceOne.

- **`GetPciDssComplianceReport`** query handler ✅ — cobre Req 1-2 (Network Security), Req 6 (Secure Systems — change management + Evidence Packs assinados), Req 10 (Log and Monitor — audit trail de releases), Req 11 (Security Testing — NotAssessed) e Req 12 (Organizational Policies).
- **Filtro por serviço e período** ✅ — query aceita `Days` (1–365) e `ServiceName` opcional.
- **2 config keys** `compliance.pci_dss.*` (sort 10270–10280) + i18n `pciDssCompliance.*` em 4 locales.
- **~11 testes unitários** ✅ — `GetPciDssComplianceReportTests.cs`.

#### H.3 Service Maturity Score v2 ✅ IMPLEMENTADO (Abril 2026)

Scorecard de maturidade v2 com 6 dimensões, pesos tier-aware e postura de vulnerabilidade integrada. Expande o modelo v1 com análise mais granular adequada a reports de Exec e gates de promoção.

- **`GetServiceMaturityScoreV2`** query handler ✅ — 6 dimensões: `ownership`, `contracts`, `documentation`, `operational_readiness`, `tier_compliance`, `vulnerability_posture`.
- **Pesos por tier** ✅ — Critical dá maior peso a `vulnerability_posture` (20%) e `tier_compliance` (20%); Experimental é mais leniente com ênfase em ownership (35%).
- **4 níveis v2** ✅ — Nascente / Em Desenvolvimento / Maduro / Excelente (thresholds: <40 / 40–65 / 65–85 / ≥85).
- **Integração com `IVulnerabilityAdvisoryRepository`** ✅ — 1 advisory Critical = `vulnerability_posture` zero.
- **3 config keys** `maturity.v2.*` (sort 10290–10310) + i18n `maturityV2.*` em 4 locales.
- **~10 testes unitários** ✅ — `ServiceMaturityV2Tests.cs`.

**Totais Wave H:** CG: 673 testes (+11). Catalog: 1754 testes (+28). Configuração: +8 config keys (sort 10240–10310). i18n: +3 secções (4 locales). **WAVE H COMPLETO**.

---

### Wave I — Compliance HIPAA + FinOps Contextual + Dependency Risk Report ✅ COMPLETO (Abril 2026)

**Objetivo:** Expandir a cobertura de compliance healthcare (HIPAA Security Rule), introduzir FinOps Contextual por serviço/equipa/ambiente (com anomaly detection), e adicionar um relatório de risco transversal do grafo de dependências de serviços.

**I.1 — `GetHipaaComplianceReport` (ChangeGovernance.Application/Compliance)**

- **5 controlos HIPAA Security Rule** (`§ 164.312(a)(1)` Access Control, `(b)` Audit Controls, `(c)(1)` Integrity, `(d)` Authentication, `(e)(1)` Transmission Security).
- **Scoring contextual** ✅ — Integrity: baseado em EvidencePacks assinados (HMAC-SHA256); Audit Controls: presença de releases no período; Authentication: parcialmente avaliado via presença de releases (proxy do controlo de autenticidade); Access Control e Transmission Security: NotAssessed (requerem fontes externas de IAM e networking).
- **Estado global** ✅ — `PartiallyCompliant` quando pelo menos 1 controlo avaliado está parcialmente conforme; `Compliant` quando todos os avaliados estão conformes; `NotAssessed` quando não há dados.
- **Filtro por serviço** ✅ — parâmetro opcional `ServiceName` para relatório de serviço específico (ex: healthcare-api).
- **2 config keys** `compliance.hipaa.*` (sort 10320–10330) + i18n `hipaaCompliance.*` em 4 locales.
- **~11 testes unitários** ✅ — `GetHipaaComplianceReportTests.cs`.

**I.2 — `ServiceCostAllocationRecord` + `IngestServiceCostRecord` + `GetServiceCostAllocationReport` + `GetFinOpsInsights` (OperationalIntelligence)**

- **`ServiceCostAllocationRecord` aggregate** ✅ — entidade central do FinOps Contextual: `TenantId`, `ServiceName`, `Environment`, `TeamId`, `DomainName`, `Category` (Compute/Storage/Network/Licensing/Observability/Other), `AmountUsd`, `Currency`, `OriginalAmount`, `PeriodStart`, `PeriodEnd`, `TagsJson`, `Source`, auditoria completa.
- **`IServiceCostAllocationRepository`** ✅ — `GetByIdAsync`, `ListByServiceAsync`, `ListByTenantAsync` (filtros: environment, category).
- **`IngestServiceCostRecord`** ✅ — comando de ingestão de registo de custo. Valida período, valor não-negativo, tenant e serviço.
- **`GetServiceCostAllocationReport`** ✅ — relatório agrupado por serviço e categoria. Suporta filtros por período, ambiente e categoria. Retorna `GrandTotalUsd` e lista de `ServiceCostSummary` com breakdown por categoria.
- **`GetFinOpsInsights`** ✅ — deteção de 3 tipos de anomalias de custo:
  - `CostOutlier`: serviço com custo acima do P75 no período.
  - `NonProdExceedsProd`: serviço onde custo de ambiente não-produtivo supera produção.
  - `CategoryGrowth`: categoria com crescimento > 20% face ao período anterior.
- **Migration** `20260421180000_OI_AddServiceCostAllocation` ✅ — tabela `ops_service_cost_allocations` com 3 índices (tenant+service+period, tenant+environment, category).
- **3 config keys** `finops.*` (sort 10340–10360) + i18n `serviceCostAllocation.*` em 4 locales.
- **~12 testes unitários** ✅ — `ServiceCostAllocationTests.cs`.

**I.3 — `GetDependencyRiskReport` (Catalog.Application/Graph)**

- **Risk scoring baseado em grafo** ✅ — cálculo de score de risco (0–100) por serviço combinando:
  - Tier do serviço: Critical = 40 base, Standard = 20, Experimental = 5.
  - Fan-in de APIs: `apiCount * 5` (cap de 30) — mais APIs expostas = maior blast radius potencial.
  - Governance gap: team name "unassigned" ou "unknown" = +15 penalidade.
- **4 níveis de risco** ✅ — Low (<35) / Medium (35–59) / High (60–79) / Critical (≥80).
- **`DependencyRiskLevel` geral** ✅ — nível mais alto entre todos os serviços analisados.
- **`RiskFactors`** ✅ — lista de fatores específicos por serviço (tier crítico, alta exposição de APIs, governance gap).
- **Filtros** ✅ — por `TierFilter` e `MaxServices` (até 200 serviços).
- **3 config keys** `dependency.risk.*` (sort 10370–10390) + i18n `dependencyRisk.*` em 4 locales.
- **~10 testes unitários** ✅ — `DependencyRiskReportTests.cs`.

**Totais Wave I:** CG: 685 testes (+12). OI: 1004 testes (+12). Catalog: 1764 testes (+10). Configuração: +8 config keys (sort 10320–10390). i18n: +3 secções (4 locales). **WAVE I COMPLETO**.

---

### Wave J — GDPR Compliance + SLO Tracking + Rollback Recommendation ✅ COMPLETO (Abril 2026)

**Objetivo:** Expandir a cobertura de compliance para regulamentação europeia (GDPR), introduzir rastreio de Service Level Objectives (SLO Tracking) com análise de tendências e adicionar um motor de recomendação de rollback baseado em scoring composto de sinais de confiança, blast radius e integridade de evidência.

**J.1 — `GetGdprComplianceReport` (ChangeGovernance.Application/Compliance)**

- **5 controlos GDPR** ✅ — Art. 5 (Principles of Processing), Art. 13 (Transparency), Art. 17 (Right to Erasure — NotAssessed por design), Art. 25 (Privacy by Design), Art. 33 (Breach Notification Readiness).
- **Scoring contextual** ✅ — Art. 25 avaliado via evidence packs assinados (HMAC-SHA256) como proxy de Privacy by Design; Art. 5 e Art. 33 avaliados via presença de releases no período; Art. 13 avaliado via contagem de evidence packs; Art. 17 sempre NotAssessed (requer integração com sistemas de dados pessoais).
- **Estado global** ✅ — `NonCompliant` se algum controlo é não conforme; `PartiallyCompliant` quando pelo menos 1 controlo está parcialmente conforme; `NotAssessed` quando não há dados.
- **Filtro por serviço** ✅ — parâmetro opcional `ServiceName` para relatório de serviço específico.
- **2 config keys** `compliance.gdpr.*` (sort 10400–10410) + i18n `gdprCompliance.*` em 4 locales.
- **~11 testes unitários** ✅ — `GetGdprComplianceReportTests.cs`.

**J.2 — `SloObservation` + `IngestSloObservation` + `GetSloComplianceSummary` + `GetSloViolationTrend` (OperationalIntelligence)**

- **`SloObservation` aggregate** ✅ — entidade central do SLO Tracking: `TenantId`, `ServiceName`, `Environment`, `MetricName`, `ObservedValue`, `SloTarget`, `PeriodStart`, `PeriodEnd`, `ObservedAt`, `Status` (Met/Warning/Breached), `Unit`, auditoria completa.
  - **Classificação automática de status** ✅ — `Met` se `observed >= target`; `Warning` se gap ≤ 10% do target; `Breached` se gap > 10% do target.
- **`ISloObservationRepository`** ✅ — `GetByIdAsync`, `ListByServiceAsync`, `ListByTenantAsync` (filtros: environment, statusFilter), `Add`.
- **`IngestSloObservation`** ✅ — comando de ingestão de observação de SLO. Valida período (end > start), target > 0 e campos obrigatórios. Retorna status classificado na resposta.
- **`GetSloComplianceSummary`** ✅ — relatório de conformidade agregado por tenant, com 4 estados globais:
  - `AllMet`: todas as observações no período cumprem o SLO.
  - `Partial`: mistura de Met/Warning.
  - `Violated`: 1 ou mais observações Breached.
  - `NoData`: sem observações no período.
  - Retorna `ComplianceRatePercent`, `TotalObservations`, `TotalViolations`.
- **`GetSloViolationTrend`** ✅ — análise de tendência de violações num período (até 90 dias), com janelas diárias de contagem de violações:
  - `Worsening`: violações recentes > 1.2× violações anteriores.
  - `Improving`: violações recentes < 0.8× violações anteriores.
  - `Stable`: variação dentro da banda.
  - `Insufficient`: dados insuficientes para análise.
  - Retorna `PeakViolationDate` (dia com mais violações) e lista de `ViolationWindow` diárias.
- **Migration** `20260421190000_OI_AddSloTracking` ✅ — tabela `ops_slo_observations` com 3 índices (tenant+service+observedAt, tenant+observedAt+status, status).
- **3 config keys** `slo.tracking.*` (sort 10420–10440) + i18n `sloTracking.*` em 4 locales.
- **~16 testes unitários** ✅ — `SloTrackingTests.cs`.

**J.3 — `GetChangeRollbackRecommendation` (ChangeGovernance.Application/ChangeIntelligence)**

- **Score composto de urgência de rollback (0–100)** ✅ — combina 3 factores:
  - **Factor 1 — ChangeConfidence** (penalty 0–40): Very Low (<30): +40; Low (30–49): +25; Moderate (50–69): +10; High (≥70): +0; sem dados: +15.
  - **Factor 2 — BlastRadius** (penalty 5–30): High (≥20 consumidores afetados): +30; Moderate (5–19): +15; Low (<5): +5; sem dados: +10.
  - **Factor 3 — EvidenceIntegrity** (penalty 0–30): por cada evidence pack não assinado +10 (cap 30); todos assinados: +0; sem evidence packs: +10.
- **4 níveis de urgência** ✅ — `None` (0–24), `Suggest` (25–49), `Recommend` (50–74), `Critical` (75–100).
- **Score clampado** ✅ — `Math.Clamp(score, 0, 100)` para nunca exceder 100.
- **`RollbackFactor`** ✅ — detalhe por fator com `FactorName`, `ScorePenalty` e `Note` explicativa.
- **Flags de disponibilidade de dados** ✅ — `HasConfidenceData`, `HasBlastRadiusData` e `EvidencePackCount` para UX contextual.
- **3 config keys** `change.rollback.*` (sort 10450–10470) + i18n `rollbackRecommendation.*` em 4 locales.
- **~10 testes unitários** ✅ — `GetChangeRollbackRecommendationTests.cs`.

**Totais Wave J:** CG: 709 testes (+24). OI: 1020 testes (+16). Configuração: +8 config keys (sort 10400–10470). i18n: +3 secções (4 locales). **WAVE J COMPLETO**.

---

### Wave K — Chaos Analytics + CMMC 2.0 + Change Frequency Heatmap ✅ COMPLETO (Abril 2026)

#### K.1 — Chaos Engineering Analytics (OperationalIntelligence)

**Feature:** `GetChaosExperimentReport` — relatório analítico de todos os experimentos de chaos engineering do tenant num período configurável.

**Domínio:** Operacional — aproveita o `ChaosExperiment` aggregate já existente (Wave B) e os repositórios `IChaosExperimentRepository` + `CreateChaosExperiment` + `ListChaosExperiments` já implementados.

**Capacidades:**
- Taxa de sucesso (Completed vs Completed+Failed)
- Distribuição por tipo de experimento (`ByType`)
- Distribuição por nível de risco (`ByRiskLevel`: Low/Medium/High)
- Distribuição de estados (`ByStatus`: Planned/Running/Completed/Failed/Cancelled)
- Top 5 serviços mais testados (`TopServices`)
- Duração média dos experimentos
- Timestamp do experimento mais recente
- Filtro por período (1–90 dias), serviço e ambiente

**Ficheiros:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Runtime/Features/GetChaosExperimentReport/GetChaosExperimentReport.cs`
- `tests/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Tests/Runtime/Application/ChaosExperimentReportTests.cs`
- **~13 testes unitários** ✅ — `ChaosExperimentReportTests.cs`

#### K.2 — CMMC 2.0 Compliance Report (ChangeGovernance)

**Feature:** `GetCmmcComplianceReport` — relatório de conformidade CMMC 2.0 Level 2 para clientes em ambiente de Contratação Federal dos EUA (Controlled Unclassified Information).

**Práticas cobertas:**
- `AC.1.001` — Access Control: Limit Access to Authorized Users
- `IA.1.076` — Identification & Authentication: Identify Information System Users
- `AU.2.041` — Audit & Accountability: Create and Retain Audit Logs (scoring por evidence packs assinados)
- `IR.2.092` — Incident Response: Establish Incident-Handling Capability
- `RM.2.141` — Risk Management: Periodically Assess Organizational Risk

**Ficheiros:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/Compliance/Features/GetCmmcComplianceReport/GetCmmcComplianceReport.cs`
- `tests/modules/changegovernance/NexTraceOne.ChangeGovernance.Tests/Compliance/GetCmmcComplianceReportTests.cs`
- **~11 testes unitários** ✅ — `GetCmmcComplianceReportTests.cs`

#### K.3 — Change Frequency Heatmap + Deployment Cadence Report (ChangeGovernance)

**Features:**
1. `GetChangeFrequencyHeatmap` — heatmap de deployments por (DayOfWeek × HourOfDay) em UTC. Revela padrões de cadência, picos de risco (e.g. sextas-feiras à tarde) e janelas preferenciais.
2. `GetDeploymentCadenceReport` — classificação DORA de cadência de deployment por serviço: HighPerformer (≥1/dia), Medium (≥1/semana), LowPerformer (<1/semana), Insufficient (sem deploys no período).

**Capacidades K.3a (Heatmap):**
- Matriz 7×24 (DayOfWeek × HourOfDay)
- Célula mais quente (`MaxCellCount`, `PeakDayOfWeek`, `PeakHourOfDay`)
- Distribuição agregada por dia da semana (`ByDayOfWeek`)
- Filtro por serviço, ambiente, período (7–90 dias)

**Capacidades K.3b (Cadence):**
- Classifica cada serviço em HighPerformer/Medium/LowPerformer/Insufficient
- `DeploysPerDay` e `DeploysPerWeek` por serviço
- Distribuição global de cadência (`Distribution`)
- Ordered by `DeploysPerDay` desc
- Filtro por equipa, ambiente, período (7–90 dias), max serviços (1–200)

**Ficheiros:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/ChangeIntelligence/Features/GetChangeFrequencyHeatmap/GetChangeFrequencyHeatmap.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/ChangeIntelligence/Features/GetDeploymentCadenceReport/GetDeploymentCadenceReport.cs`
- `tests/modules/changegovernance/NexTraceOne.ChangeGovernance.Tests/ChangeIntelligence/Application/Features/ChangeFrequencyTests.cs`
- **~13 testes unitários** ✅ — `ChangeFrequencyTests.cs`

#### Configuração Wave K

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `chaos.analytics.report_lookback_days` | 30 | 10480 | Período padrão para relatório de chaos analytics |
| `chaos.analytics.max_experiments_in_report` | 500 | 10490 | Máximo de experimentos por relatório de analytics |
| `compliance.cmmc.enabled` | true | 10500 | Ativa relatório CMMC 2.0 Level 2 |
| `compliance.cmmc.report_period_days` | 90 | 10510 | Período padrão de avaliação CMMC |
| `change.frequency.heatmap.max_days` | 90 | 10520 | Máximo de dias para heatmap de frequência |
| `change.frequency.cadence.high_performer_threshold` | 1.0 | 10530 | Threshold de deploys/dia para HighPerformer |
| `change.frequency.cadence.low_performer_threshold` | 0.0357 | 10540 | Threshold mínimo de deploys/dia para LowPerformer |
| `change.frequency.cadence.max_services` | 50 | 10550 | Máximo de serviços no relatório de cadência |

#### i18n Wave K

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `chaosExperimentReport.*` — relatório de chaos engineering
- `cmmcCompliance.*` — CMMC 2.0 compliance
- `changeFrequency.heatmap.*` + `changeFrequency.cadence.*` — heatmap e cadência

**Totais Wave K:** CG: 734 testes (+25). OI: 1033 testes (+13). Configuração: +8 config keys (sort 10480–10550). i18n: +3 secções (4 locales). **WAVE K COMPLETO**.

---

### Wave L — Service Ownership Health + FedRAMP Moderate + Operational Readiness ✅ COMPLETO (Abril 2026)

#### L.1 — GetServiceOwnershipHealthReport (Catalog)

**Objetivo:** scorecard de saúde de ownership do catálogo de serviços, identificando gaps de governança que comprometem o Source of Truth do NexTraceOne.

**Implementação:**
- `GetServiceOwnershipHealthReport.Query` — filtros por tenant, tier, threshold de saúde, máximo de serviços e threshold de staleness de revisão de ownership
- `OwnershipIssue` enum — `MissingTeam`, `MissingTechnicalOwner`, `MissingBusinessOwner`, `StaleReview`, `MissingDocumentation`
- `OwnershipHealthBand` enum — `Healthy` (≥90), `Fair` (≥70), `AtRisk` (≥50), `Critical` (<50)
- Score por serviço (0–100): -35 equipa ausente, -25 tech owner ausente, -15 biz owner ausente, -15 revisão stale, -10 doc URL ausente
- Score global do catálogo (média dos serviços analisados)
- `OwnershipIssueBreakdown` — contagem de cada tipo de problema por serviço
- Ordenação por score ascendente (piores primeiro)
- Filtro por `HealthScoreThreshold` para focar nos serviços mais problemáticos
- Orientado para **Tech Lead, Architect e Platform Admin** personas

#### L.2 — GetFedRampComplianceReport (ChangeGovernance)

**Objetivo:** relatório de conformidade FedRAMP Moderate para clientes cloud federais dos EUA, adicionando à suite de compliance o standard NIST SP 800-53 Rev 5.

**Controlos FedRAMP Moderate cobertos:**
- **AC-2** (Access Control): Account Management — deploy workflow como proxy de controlo de acesso
- **AU-2** (Audit & Accountability): Event Logging — evidence packs HMAC-SHA256 como artefatos de auditoria
- **CM-6** (Configuration Management): Configuration Settings — releases com version tracking de configuração
- **IR-4** (Incident Response): Incident Handling — correlação change-to-incident e rollback intelligence
- **SI-2** (System & Information Integrity): Flaw Remediation — vulnerability promotion gates por release

**Implementação:**
- `FedRampControlResult` record — `ControlId`, `ControlFamily`, `ControlName`, `Status`, `Note`
- `ImpactLevel: "Moderate"` identifica o baseline de autorização FedRAMP
- Scoring contextual via releases e evidence packs assinados (HMAC-SHA256)
- Overall status: `NotAssessed → PartiallyCompliant → Compliant`
- Reutiliza `Nis2ControlStatus` enum da suite de compliance existente

#### L.3 — GetOperationalReadinessReport (OperationalIntelligence)

**Objetivo:** scorecard composto de prontidão operacional pré-produção, combinando 5 dimensões operacionais para produzir um score e classificação de readiness.

**5 Dimensões com pesos:**
| Dimensão | Peso | Fonte de dados |
|---|---|---|
| SLO Compliance | 35% | `SloObservation` (% Met) |
| Chaos Resilience | 25% | `ChaosExperiment` (% Completed) |
| Drift Free | 20% | `DriftFinding` (unacknowledged) |
| Profiling Coverage | 10% | `ProfilingSession` (recente) |
| Baseline Coverage | 10% | `RuntimeSnapshot` (recente) |

**Classificações:**
- `ReadyForProduction` — score ≥ 80 e zero bloqueadores
- `ConditionallyReady` — score ≥ 60 mas com bloqueadores
- `NotReady` — score < 60 ou bloqueadores críticos

**Bloqueadores automáticos:**
- SLO breaches no período de lookback
- Chaos experiments que falharam
- Drift findings não reconhecidos
- Ausência de sessão de profiling recente
- Ausência de baseline de runtime recente

**Orientado para Tech Lead, Engineer e Platform Admin** — suporta gates de promoção pré-produção.

#### Configuração Wave L

```
ownership.health.report.staleness_threshold_days  sort 10560  default: 180
ownership.health.report.max_services              sort 10570  default: 100
compliance.fedramp.enabled                        sort 10580  default: true
compliance.fedramp.report_period_days             sort 10590  default: 90
operational.readiness.lookback_days               sort 10600  default: 30
operational.readiness.ready_score_threshold       sort 10610  default: 80
operational.readiness.conditional_score_threshold sort 10620  default: 60
operational.readiness.slo_weight_percent          sort 10630  default: 35
```

#### i18n Wave L

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `ownershipHealth.*` — scorecard de ownership de serviços
- `fedRampCompliance.*` — FedRAMP Moderate compliance
- `operationalReadiness.*` — prontidão operacional pré-produção

**Totais Wave L:** CG: 745 testes (+11). OI: 1047 testes (+14). Catalog: 1783 testes (+19). Configuração: +8 config keys (sort 10560–10630). i18n: +3 secções (4 locales). **WAVE L COMPLETO**.

---

### Wave M — Contract Health Distribution + Team Change Velocity + Open Drift Impact

#### M.1 — GetContractHealthDistributionReport (Catalog Contracts)

**Feature:** Relatório de distribuição de scores de saúde de todos os contratos de API registados no tenant.

**Produz:**
- Contagem total e percentagem por banda de saúde: `Healthy / Fair / AtRisk / Critical`
- Score médio por dimensão: breaking change frequency, consumer impact, review recency, example coverage, policy compliance, documentation, overall
- Lista dos contratos mais críticos (score mais baixo), limitada por `TopCriticalCount`
- Percentagem de contratos saudáveis e críticos no catálogo

**Bandas de saúde (configuráveis):**
- `Healthy` — score ≥ HealthyThreshold (default 80)
- `Fair` — score ≥ FairThreshold (default 60)
- `AtRisk` — score ≥ AtRiskThreshold (default 40)
- `Critical` — score < AtRiskThreshold

**Orientado para Architect, Tech Lead e Platform Admin** — serve como painel de qualidade dos contratos e fonte de verdade da saúde do catálogo.

#### M.2 — GetTeamChangeVelocityReport (ChangeGovernance Change Intelligence)

**Feature:** Relatório de velocidade de mudança por equipa para um período configurável.

**Produz por equipa:**
- Total de releases
- Releases por semana (velocidade média)
- Taxa de sucesso (`Succeeded / total`)
- Taxa de falha (`Failed / total`)
- Taxa de rollback (`RolledBack / total`)
- Participação percentual no total do tenant
- Classificação de nível de velocidade

**Níveis de velocidade (`VelocityTier`):**
- `HighVolume` — ≥ 4 releases/semana
- `Moderate` — ≥ 1 release/semana
- `LowFrequency` — ≥ 0.25 releases/semana
- `Inactive` — < 0.25 releases/semana

**Métricas tenant-level:** taxa de sucesso global, taxa de rollback global, equipas com rollbacks no período.

**Orientado para Tech Lead, Architect e Executive** — compara cadência e confiabilidade entre equipas e identifica outliers.

#### M.3 — GetOpenDriftImpactSummary (OperationalIntelligence Runtime)

**Feature:** Sumário de impacto dos drift findings abertos (não reconhecidos, não resolvidos).

**Produz:**
- Total de drifts abertos
- Desvio médio global e desvio máximo global
- Distribuição de severidade: `Low / Medium / High / Critical` (por percentagem de desvio)
- Top serviços mais afetados (por contagem de drifts, com desvio máx e médio e pior severidade)
- Top métricas mais desviantes (por desvio médio, com serviços afetados)

**Classificação de severidade por desvio:**
- `Low` — desvio < 10%
- `Medium` — desvio 10–30%
- `High` — desvio 30–60%
- `Critical` — desvio > 60%

**Orientado para Tech Lead, Engineer e Platform Admin** — suplementa o `GetOperationalReadinessReport` com detalhe sobre drifts abertos e identifica hotspots de instabilidade operacional.

#### Configuração Wave M

```
contracts.health.distribution.top_critical_count  sort 10640  default: 10
contracts.health.distribution.healthy_threshold   sort 10650  default: 80
contracts.health.distribution.fair_threshold      sort 10660  default: 60
changes.velocity.lookback_days                    sort 10670  default: 90
changes.velocity.top_teams_count                  sort 10680  default: 20
drift.impact.summary.max_services                 sort 10690  default: 10
drift.impact.summary.max_metrics                  sort 10700  default: 10
drift.impact.summary.page_size                    sort 10710  default: 200
```

#### i18n Wave M

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `contractHealthDistribution.*` — distribuição de saúde de contratos
- `teamChangeVelocity.*` — velocidade de mudanças por equipa
- `openDriftImpact.*` — sumário de impacto de drifts abertos

**Totais Wave M:** CG: 760 testes (+15). OI: 1068 testes (+21). Catalog: 1802 testes (+19). Configuração: +8 config keys (sort 10640–10710). i18n: +3 secções (4 locales). **WAVE M COMPLETO**.

---

### Wave S — Change Window Utilization + Contract Adoption + MTTR Trend ✅

**Objetivo:** Fechar o loop entre Release Calendar e conformidade de deployment, medir a velocidade de adoção de versões de contrato pelos consumidores, e introduzir análise de tendência de MTTR (Mean Time To Restore) por serviço.

#### S.1 — GetChangeWindowUtilizationReport (ChangeGovernance)

**Feature:** Relatório de utilização de janelas de mudança registadas no Release Calendar. Responde à pergunta: "as equipas estão a respeitar as janelas de deployment definidas?"

**Domínio:** Aproveita `ReleaseCalendarEntry` (Wave F) e os repositórios de `Release` já existentes.

**Capacidades:**
- Para cada janela do tipo `Scheduled` ou `HotfixAllowed` no período, calcula quantas releases ocorreram dentro (`InsideWindow`) e fora (`OutsideWindow`) da janela
- **Taxa de compliance global:** `InsideWindow / TotalReleases * 100`
- **Distribuição por tipo de janela:** Scheduled / HotfixAllowed / Maintenance
- **Top equipas não-conformes:** equipas com maior taxa de deploys fora de janela
- **Classificação de compliance:**
  - `Excellent` — taxa ≥ `excellent_threshold` (default 90%)
  - `Good` — taxa ≥ `good_threshold` (default 70%)
  - `AtRisk` — taxa < `good_threshold`
- Filtro por ambiente e período (7–90 dias)

**Orientado para Platform Admin, Tech Lead e Architect** — alimenta o Release Calendar como fonte de verdade de compliance operacional.

#### S.2 — GetContractAdoptionReport (Catalog)

**Feature:** Progresso de adoção de versões de contrato pelos consumidores registados. Identifica contratos com migração lenta e consumidores presos em versões antigas.

**Domínio:** Cruza `ApiAsset` + `ContractVersion` + `ConsumerExpectation` (consumidores registados por versão).

**Capacidades:**
- Para cada contrato com múltiplas versões e consumidores registados, calcula:
  - `ConsumersOnLatestVersion` / `TotalConsumers` = **taxa de migração**
  - Versão mais antiga ainda em uso (`OldestActiveVersion`) — identifica "stragglers"
  - `DaysLatestVersionAge` — antiguidade da versão mais recente
- **MigrationTier por contrato:**
  - `Complete` — taxa ≥ 90% (todos os consumidores na versão mais recente)
  - `InProgress` — taxa ≥ 50%
  - `Lagging` — taxa < 50%
  - `NoConsumers` — sem expectativas de consumo registadas
- **Top contratos com migração mais lenta** (ordenados por taxa asc)
- **Distribuição global de MigrationTier** no tenant
- Filtro por protocolo (OpenAPI/AsyncAPI/GraphQL/Protobuf) e ambiente

**Orientado para Architect e Tech Lead** — suporta decisões de deprecation e sunset de versões com visibilidade de impacto real.

#### S.3 — GetMttrTrendReport (OperationalIntelligence)

**Feature:** Tendência de MTTR (Mean Time To Restore) por serviço e equipa. Mede a velocidade de restauro de incidentes ao longo do tempo e compara com benchmarks DORA.

**Domínio:** Cruza `Incident` (`CreatedAt` → `ResolvedAt`) agrupado por `ServiceName`/`TeamId`.

**Capacidades:**
- **MTTR médio por serviço** no período (em minutos)
- **Classificação DORA de MTTR:**
  - `Elite` — MTTR < 60 min
  - `High` — MTTR < 240 min (4h)
  - `Medium` — MTTR < 1440 min (24h)
  - `Low` — MTTR ≥ 1440 min
- **Distribuição global por DORA tier** no tenant
- **Tendência em janelas diárias** (`TrendWindows`): 30 pontos com data + MTTR médio do dia
- **Classificação de tendência:**
  - `Worsening` — MTTR recente > 1.25× MTTR anterior
  - `Improving` — MTTR recente < 0.75× MTTR anterior
  - `Stable` — variação dentro da banda
  - `Insufficient` — dados insuficientes (< 2 incidentes)
- **Top serviços com pior MTTR** e **top serviços com maior pioria de MTTR** no período
- Filtro por equipa, ambiente e período (7–90 dias)

**Orientado para Tech Lead, Engineer e Platform Admin** — alinha com DORA metrics e suporta objetivos de confiabilidade operacional.

#### Configuração Wave S

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `changes.window_utilization.lookback_days` | 30 | 11120 | Período padrão para relatório de utilização de janelas |
| `changes.window_utilization.excellent_threshold` | 90 | 11130 | Threshold (%) para classificação Excellent de compliance |
| `changes.window_utilization.good_threshold` | 70 | 11140 | Threshold (%) para classificação Good de compliance |
| `contracts.adoption.max_contracts` | 50 | 11150 | Máximo de contratos no relatório de adoção |
| `contracts.adoption.migration_complete_threshold` | 90 | 11160 | Threshold (%) de consumidores na versão mais recente para MigrationTier Complete |
| `contracts.adoption.migration_in_progress_threshold` | 50 | 11170 | Threshold (%) para MigrationTier InProgress |
| `runtime.mttr.lookback_days` | 30 | 11180 | Período padrão para análise de MTTR |
| `runtime.mttr.trend_window_count` | 30 | 11190 | Número de janelas diárias na tendência de MTTR |

#### i18n Wave S

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `changeWindowUtilization.*` — conformidade de janelas de mudança
- `contractAdoption.*` — adoção de versões de contratos por consumidores
- `mttrTrend.*` — tendência de Mean Time To Restore

**Totais reais Wave S:** CG: 866 testes (+14). Catalog: 1873 testes (+12). OI: 1178 testes (+17). Configuração: +8 config keys (sort 11120–11190). i18n: +3 secções (4 locales). **Wave S COMPLETA.**

---

### Wave T — Post-Incident Learning + API Schema Coverage + Environment Stability ✅ COMPLETA

**Objetivo:** Medir o aprendizado organizacional pós-incidente, avaliar a completude de documentação dos schemas de API e monitorizar a estabilidade comparada de ambientes para suportar decisões de promoção entre ambientes.

#### T.1 — GetPostIncidentLearningReport (ChangeGovernance)

**Feature:** Relatório de aprendizado pós-incidente. Quantifica em que medida os incidentes geram conhecimento documentado (runbooks, notas) que previne recorrência.

**Domínio:** Cruza `Incident` com `ProposedRunbook` (Knowledge module via abstração) e identifica incidentes repetidos no mesmo serviço.

**Capacidades:**
- Para cada incidente no período, verifica se foi criado um `ProposedRunbook` com status `Approved` pós-incidente
- **Learning Rate:** `IncidentsWithRunbook / TotalIncidents * 100`
- **Incidentes recorrentes:** mesmo serviço + mesmo `Category` com ≥ 2 ocorrências sem runbook aprovado = "recorrência sem aprendizado"
- **Classificação de cobertura:**
  - `Full` — taxa ≥ 80%
  - `Partial` — taxa ≥ 40%
  - `Low` — taxa < 40%
- **Top serviços com menor learning rate** (piores primeiro)
- **Total de incidentes recorrentes não documentados** (risco de reincidência)
- Filtro por equipa, serviço e período (7–180 dias)

**Orientado para Tech Lead, Architect e Platform Admin** — reforça o Knowledge Hub como ferramenta de melhoria contínua e não apenas repositório passivo.

#### T.2 — GetApiSchemaCoverageReport (Catalog)

**Feature:** Scorecard de completude de documentação de schemas de API. Revela contratos mal documentados que comprometem a qualidade do catálogo como Source of Truth.

**Domínio:** Analisa `ApiAsset` + `ContractVersion` em estado `Approved` ou `Locked`.

**Capacidades:**
- Para cada contrato, score de completude de schema (0–100) por 4 dimensões:
  - +25 **Response bodies documentados** (pelo menos 200 OK definido)
  - +25 **Request body documentado** (onde aplicável — não GET/DELETE)
  - +25 **Exemplos de payload presentes** (pelo menos 1 exemplo por contrato)
  - +25 **Status codes complementares documentados** (além de 200: 4xx, 5xx, 201/204)
- **CoverageGrade por contrato:**
  - `A` — score ≥ 90
  - `B` — score ≥ 70
  - `C` — score ≥ 50
  - `D` — score < 50
- **Distribuição global por CoverageGrade** no tenant
- **Score médio de cobertura de schema** do tenant
- **Top contratos com menor cobertura** (prioridade para melhoria)
- Filtro por protocolo, equipa e máximo de contratos (1–200)

**Orientado para Architect e Tech Lead** — alimenta developer onboarding e garante que o catálogo tenha valor real para consumidores de contratos.

#### T.3 — GetEnvironmentStabilityReport (OperationalIntelligence)

**Feature:** Score de estabilidade comparado por ambiente (dev/staging/production). Responde à pergunta: "os ambientes não-produtivos são estáveis o suficiente para ser confiáveis como preditores de produção?"

**Domínio:** Agrega sinais de `SloObservation` (breaches), `DriftFinding` (não reconhecidos), `ChaosExperiment` (falhas) e incidentes correlacionados a mudanças por ambiente.

**Capacidades:**
- **Score de estabilidade por ambiente (0–100)** com 4 dimensões ponderadas:
  - SLO compliance (30%): `MetCount / TotalObservations`
  - Drift-free ratio (30%): `1 - (OpenDriftFindings / MaxExpected)`
  - Chaos success (20%): `CompletedExperiments / TotalExperiments`
  - Post-change incident ratio (20%): `1 - (IncidentsPostChange / TotalReleases)`
- **Classificação por ambiente:**
  - `Stable` — score ≥ 80
  - `Unstable` — score ≥ 50
  - `Critical` — score < 50
- **Sinal de alerta "Non-Prod mais instável que Prod"** — flag explícita quando staging/dev tem score inferior ao de production (inverte o valor preditivo)
- **Top serviços desestabilizadores** por ambiente (maior contribuição negativa)
- Comparação side-by-side entre todos os ambientes ativos do tenant

**Orientado para Tech Lead, Engineer e Platform Admin** — base para gates de promoção que consideram estabilidade de ambientes upstream.

#### Configuração Wave T

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `compliance.learning.lookback_days` | 90 | 11200 | Período de análise para relatório de aprendizado pós-incidente |
| `compliance.learning.full_coverage_threshold` | 80 | 11210 | Threshold (%) para classificação Full de cobertura |
| `compliance.learning.partial_coverage_threshold` | 40 | 11220 | Threshold (%) para classificação Partial de cobertura |
| `contracts.schema_coverage.max_contracts` | 100 | 11230 | Máximo de contratos no relatório de cobertura de schema |
| `contracts.schema_coverage.grade_a_threshold` | 90 | 11240 | Threshold de score para CoverageGrade A |
| `contracts.schema_coverage.grade_b_threshold` | 70 | 11250 | Threshold de score para CoverageGrade B |
| `runtime.environment_stability.lookback_days` | 30 | 11260 | Período de análise para estabilidade de ambientes |
| `runtime.environment_stability.stable_threshold` | 80 | 11270 | Threshold de score para classificação Stable |

#### i18n Wave T

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `postIncidentLearning.*` — aprendizado pós-incidente e runbook coverage
- `apiSchemaCoverage.*` — cobertura de documentação de schemas de API
- `environmentStability.*` — estabilidade comparada de ambientes

**Totais reais Wave T:** CG: 879/879 testes (+13). Catalog: 1885/1885 testes (+12). OI: 1192/1192 testes (+14). Configuração: +8 config keys (sort 11200–11270). i18n: +3 secções (4 locales). **Wave T TOTALMENTE COMPLETA.**

---

### Wave U — Compliance Coverage Matrix + Dependency Freshness + Service Load Distribution

**Objetivo:** Introduzir uma visão unificada de cobertura de compliance por serviço (matriz multi-standard), medir o envelhecimento das dependências entre serviços cruzado com vulnerabilidades, e mapear a distribuição de carga operacional para identificar outliers de custo por throughput.

#### U.1 — GetComplianceCoverageMatrixReport (ChangeGovernance)

**Feature:** Matriz de cobertura de standards de compliance por serviço ou equipa. Identifica "compliance blind spots" — serviços sem qualquer avaliação de conformidade.

**Domínio:** Agrega resultados de todos os relatórios de compliance (SOC2, ISO 27001, PCI-DSS, HIPAA, GDPR, FedRAMP, NIS2, CMMC) por serviço/tenant.

**Capacidades:**
- Para cada serviço no tenant, lista quais standards foram avaliados e respetivo estado (`Compliant / PartiallyCompliant / NonCompliant / NotAssessed`)
- **CoverageScore por serviço:** número de standards com estado diferente de `NotAssessed` / total de standards ativos × 100
- **CoverageLevel por serviço:**
  - `Full` — todos os standards ativos avaliados (score = 100%)
  - `Partial` — ≥ 50% dos standards avaliados
  - `Minimal` — < 50% avaliados
  - `None` — nenhum standard avaliado (blind spot)
- **Distribuição global de CoverageLevel** no tenant
- **Top serviços com maior gap de compliance** (piores primeiros por CoverageScore)
- **Score de compliance agregado por standard** (quantos serviços passaram em cada standard)
- Filtro por equipa, tier de serviço e lista de standards ativos configurável

**Orientado para Auditor, Platform Admin e Executive** — visão transversal que responde "quão cobertos estamos em compliance?" em vez de relatório por standard individual.

#### U.2 — GetDependencyUpdateFreshnessReport (Catalog)

**Feature:** Análise de "frescor" das dependências registadas entre serviços. Identifica serviços com dependências desatualizadas que aumentam o risco de exposição a vulnerabilidades conhecidas.

**Domínio:** Cruza `ServiceDependency` (última mudança via `ContractChangelog` ou `Release`) com `VulnerabilityAdvisoryRecord` por serviço.

**Capacidades:**
- Para cada serviço, calcula `DaysSinceLastDependencyChange` (último `ContractChangelog` de um contrato consumido pelo serviço, ou último `Release` que tocou no serviço)
- **FreshnessTier por serviço:**
  - `Fresh` — últimos 30 dias
  - `Aging` — 31–90 dias
  - `Stale` — 91–180 dias
  - `Critical` — mais de 180 dias sem atualização registada
- **Correlação com vulnerabilidades:** serviços `Stale` ou `Critical` com `VulnerabilityAdvisoryRecord` aberto = flag `VulnerabilityGap`
- **Top serviços mais desatualizados** com número de vulns abertas associadas
- **Distribuição global por FreshnessTier** no tenant
- Filtro por tier de serviço e threshold de criticidade de vulnerabilidade

**Orientado para Architect, Tech Lead e Security** — alimenta decisões de upgrade de dependências priorizadas por risco real.

#### U.3 — GetServiceLoadDistributionReport (OperationalIntelligence)

**Feature:** Distribuição de carga operacional entre serviços do tenant, correlacionada com custo. Identifica outliers de custo por throughput (serviços caros com baixo uso — desperdício) e serviços sobrecarregados.

**Domínio:** Cruza `RuntimeSnapshot` (latência, throughput, taxa de erro) com `ServiceCostAllocationRecord` (custo por serviço/ambiente).

**Capacidades:**
- Para cada serviço com snapshots de runtime no período, calcula:
  - `AvgLatencyMs`, `AvgThroughput`, `AvgErrorRate` (médias dos snapshots)
  - `TotalCostUsd` (do `ServiceCostAllocationRecord` no mesmo período e ambiente)
  - `CostPerRequestUsd` = `TotalCostUsd / (AvgThroughput * PeriodDays * 86400)` — custo por request
- **LoadBand por serviço** (baseado em throughput comparativo por quartil):
  - `HighLoad` — throughput no quartil superior (top 25%)
  - `MediumLoad` — quartis 2 e 3 (50%)
  - `LowLoad` — quartil inferior (bottom 25%)
- **WasteCandidate flag** — serviços `LowLoad` com custo > mediana dos pares (custo elevado, uso baixo)
- **HighCostEfficiency flag** — serviços `HighLoad` com `CostPerRequest` abaixo da mediana (eficientes)
- **Top 10 serviços com pior custo por request** (outliers de waste)
- Filtro por ambiente e período (7–90 dias)

**Orientado para Platform Admin, FinOps e Architect** — fecha o loop entre observabilidade operacional e custo real, habilitando decisões de rightsizing.

#### Configuração Wave U

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `compliance.coverage.enabled_standards` | `SOC2,ISO27001,PCI-DSS,HIPAA,GDPR,FedRAMP,NIS2,CMMC` | 11280 | Standards ativos para a matriz de cobertura |
| `compliance.coverage.full_threshold` | 100 | 11290 | Threshold (%) para CoverageLevel Full |
| `compliance.coverage.partial_threshold` | 50 | 11300 | Threshold (%) para CoverageLevel Partial |
| `catalog.dependency_freshness.fresh_threshold_days` | 30 | 11310 | Máximo de dias para FreshnessTier Fresh |
| `catalog.dependency_freshness.aging_threshold_days` | 90 | 11320 | Máximo de dias para FreshnessTier Aging |
| `catalog.dependency_freshness.stale_threshold_days` | 180 | 11330 | Máximo de dias para FreshnessTier Stale |
| `runtime.load_distribution.max_services` | 50 | 11340 | Máximo de serviços no relatório de distribuição de carga |
| `runtime.load_distribution.lookback_days` | 30 | 11350 | Período de análise para distribuição de carga |

#### i18n Wave U

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `complianceCoverageMatrix.*` — matriz de cobertura de compliance por serviço
- `dependencyFreshness.*` — frescor de dependências e gap de vulnerabilidades
- `serviceLoadDistribution.*` — distribuição de carga operacional e custo por request

**Totais estimados Wave U:** CG: ~892 testes (+13). Catalog: ~1898 testes (+11). OI: ~1210 testes (+15). Configuração: +8 config keys (sort 11280–11350). i18n: +3 secções (4 locales).

---

### Wave V — API Growth Rate + Chaos Coverage Gap + Release Frequency Deviation

**Objetivo:** Detectar acumulação descontrolada de APIs em serviços (crescimento sem governance), identificar serviços críticos sem cobertura de chaos engineering, e revelar desvios bruscos de ritmo de deployment que podem indicar problemas organizacionais ou de confiança.

#### V.1 — GetServiceApiGrowthReport (Catalog)

**Feature:** Taxa de crescimento do número de APIs (contratos) por serviço ao longo do tempo. Identifica serviços a acumular APIs sem revisão de governance.

**Domínio:** Compara `ApiAssetCount` por serviço entre dois períodos (atual vs. `comparison_period_days` atrás), cruzado com `GetContractHealthDistributionReport` para correlacionar crescimento com qualidade.

**Capacidades:**
- Para cada serviço, compara contagem de contratos em `Approved` ou `Locked` entre o período atual e o período de comparação
- **GrowthRatePct:** `(CurrentCount - PreviousCount) / PreviousCount * 100`
- **GrowthTier por serviço:**
  - `Stable` — crescimento < 10%
  - `Growing` — crescimento 10–50%
  - `RapidGrowth` — crescimento 50–100%
  - `Exploding` — crescimento > 100% (risco de governance sprawl)
  - `Shrinking` — crescimento negativo (consolidação ou deprecation)
- **GovernanceRisk flag** — serviços `RapidGrowth` ou `Exploding` com `ContractHealthScore` médio < 60 (crescimento com baixa qualidade)
- **Top serviços com maior crescimento** e top serviços `GovernanceRisk`
- **Distribuição global por GrowthTier** no tenant
- Filtro por equipa e tier de serviço

**Orientado para Architect e Platform Admin** — previne "API sprawl" e garante que o crescimento do catálogo acompanha a governance.

#### V.2 — GetChaosCoverageGapReport (OperationalIntelligence)

**Feature:** Análise de gaps de cobertura de chaos engineering por serviço. Identifica serviços críticos não testados quanto à sua resiliência.

**Domínio:** Cruza `ServiceAsset` (tier, owner) com `ChaosExperiment` (por serviço, por ambiente) para identificar ausências de cobertura.

**Capacidades:**
- Para cada serviço no tenant, verifica presença de `ChaosExperiment` no período:
  - Sem experimentos = `NoCoverage`
  - Experimentos apenas em não-produção = `ProductionGap`
  - Experimentos em produção mas todos `Failed` ou `Cancelled` = `FailedCoverage`
  - Experimentos com pelo menos 1 `Completed` em produção = `FullCoverage`
- **GapLevel enum:** `NoCoverage / ProductionGap / FailedCoverage / PartialCoverage / FullCoverage`
- **CriticalGap flag** — serviços de tier `Critical` com `GapLevel != FullCoverage`
- **Distribuição global por GapLevel** no tenant
- **Top serviços críticos sem cobertura** (ordenados por tier e nome)
- **CoverageRate geral** = serviços com `FullCoverage` / total de serviços ativos
- Filtro por tier de serviço, ambiente e período (30–365 dias)

**Orientado para Architect, Tech Lead e Platform Admin** — suporta a estratégia de chaos engineering como capacidade governada, não ad-hoc.

#### V.3 — GetReleaseFrequencyDeviationReport (ChangeGovernance)

**Feature:** Deteção de desvios bruscos de frequência de deployment por serviço. Identifica acelerações (possível rush sem qualidade) ou desacelerações (possível estagnação ou bloqueio).

**Domínio:** Compara `DeploysPerDay` em dois períodos: recente (`recent_days`) vs. histórico (`historical_days`). Aprofunda a análise de cadência do `GetDeploymentCadenceReport` (Wave K) com perspetiva temporal.

**Capacidades:**
- Para cada serviço com releases no histórico, calcula:
  - `DeploysPerDayRecent` = releases nos últimos `recent_days` / `recent_days`
  - `DeploysPerDayHistorical` = releases nos `historical_days` anteriores / `historical_days`
  - `DeviationPct` = `(recent - historical) / historical * 100`
- **FrequencyDeviation enum:**
  - `Accelerating` — desvio > +50% (rush de deployments — risco de qualidade)
  - `Stable` — desvio entre -50% e +50%
  - `Decelerating` — desvio < -50% (possível bloqueio ou loss of momentum)
  - `Stalled` — zero releases recentes mas com histórico (potencial paralisia)
  - `New` — sem histórico, apenas releases recentes (serviço novo)
- **RiskFlag** — `Accelerating` com `ReleaseSuccessRate < 80%` ou `Stalled` com tier `Critical`
- **Top serviços com maior desvio positivo e negativo**
- **Distribuição global por FrequencyDeviation** no tenant
- Filtro por equipa, ambiente e tier de serviço

**Orientado para Tech Lead, Architect e Executive** — complementa a análise DORA de cadência com perspetiva de variação de ritmo, não só estado absoluto.

#### Configuração Wave V

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `catalog.api_growth.comparison_period_days` | 90 | 11360 | Período de comparação para cálculo de crescimento de APIs |
| `catalog.api_growth.stable_threshold_pct` | 10 | 11370 | Threshold (%) de crescimento para GrowthTier Stable |
| `catalog.api_growth.rapid_threshold_pct` | 50 | 11380 | Threshold (%) de crescimento para GrowthTier RapidGrowth |
| `chaos.coverage.lookback_days` | 90 | 11390 | Período de análise para gaps de cobertura de chaos |
| `chaos.coverage.max_services` | 100 | 11400 | Máximo de serviços no relatório de gap de chaos |
| `changes.frequency_deviation.historical_days` | 90 | 11410 | Período histórico para cálculo de frequência base |
| `changes.frequency_deviation.recent_days` | 30 | 11420 | Período recente para cálculo de frequência de comparação |
| `changes.frequency_deviation.max_services` | 50 | 11430 | Máximo de serviços no relatório de desvio de frequência |

#### i18n Wave V

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `serviceApiGrowth.*` — taxa de crescimento de APIs por serviço
- `chaosCoverageGap.*` — gaps de cobertura de chaos engineering
- `releaseFrequencyDeviation.*` — desvio de frequência de deployments

**Totais estimados Wave V:** CG: ~903 testes (+11). Catalog: ~1911 testes (+13). OI: ~1224 testes (+14). Configuração: +8 config keys (sort 11360–11430). i18n: +3 secções (4 locales).

---

### Wave W — Rollback Pattern Analysis + Service Coupling Index + Anomaly Detection Summary

**Objetivo:** Analisar padrões sistemáticos de rollback para identificar anti-padrões de deployment, introduzir um índice de acoplamento entre serviços baseado em dependências registadas, e consolidar todas as anomalias detetadas pelo sistema numa visão unificada por serviço.

#### W.1 — GetRollbackPatternReport (ChangeGovernance)

**Feature:** Análise de padrões de rollback por serviço, equipa e ambiente. Distingue rollbacks isolados de padrões recorrentes ou seriais que indicam disfunções sistémicas.

**Domínio:** Agrega `Release` com `DeploymentStatus = RolledBack` cruzado com `ChangeConfidenceBreakdown` e `EvidencePack` para correlacionar rollbacks com qualidade de preparação.

**Capacidades:**
- Para cada serviço com rollbacks no período, calcula:
  - `TotalRollbacks` e `RollbackRate` = rollbacks / total releases
  - `AvgConfidenceAtRollback` — confidence score médio das releases que foram revertidas
  - `AvgEvidencePackCompleteness` — completude média dos evidence packs nas releases revertidas
- **RollbackPattern por serviço:**
  - `Isolated` — 1 rollback no período (one-off)
  - `Recurring` — 2–3 rollbacks no período (padrão de atenção)
  - `Serial` — ≥ 4 rollbacks no período (disfunção sistémica)
  - `None` — zero rollbacks (clean record)
- **SystemicRisk flag** — serviços `Serial` com `AvgConfidenceAtRollback < 50` (deploys de baixa confiança que resultam em rollback)
- **Correlação com Evidence Packs** — serviços com rollbacks e `AvgEvidencePackCompleteness < 70%` = flag `EvidenceGap`
- **Top serviços com maior RollbackRate** e top serviços `Serial`
- **Distribuição por RollbackPattern** no tenant
- Filtro por equipa, ambiente e período (7–180 dias)

**Orientado para Tech Lead, Architect e Platform Admin** — fecha o loop entre rollback intelligence (Wave J.3) e análise de padrões de qualidade de deployment.

#### W.2 — GetServiceCouplingIndexReport (Catalog)

**Feature:** Índice de acoplamento estrutural entre serviços do tenant baseado em dependências registadas. Identifica "hub services" de alta criticidade e serviços isolados sem integração no ecossistema.

**Domínio:** Analisa `ServiceDependency` (grafo dirigido de dependências) para calcular fan-in (quantos dependem de mim) e fan-out (de quantos eu dependo) por serviço.

**Capacidades:**
- Para cada serviço, calcula:
  - `FanIn` — número de serviços que dependem deste serviço (upstream consumers)
  - `FanOut` — número de serviços dos quais este serviço depende (downstream dependencies)
  - `CouplingIndex` = normalizado 0–100: `min(100, (FanIn * 3 + FanOut * 2) / sqrt(max(1, TotalServices)) * 10)`
- **CouplingTier por serviço:**
  - `HubService` — CouplingIndex ≥ 70 (alto fan-in — alto blast radius potencial)
  - `HighlyCoupled` — CouplingIndex ≥ 50 (alto fan-out — alta dependência de terceiros)
  - `ModeratelyCoupled` — CouplingIndex ≥ 25
  - `LooselyCoupled` — CouplingIndex ≥ 10
  - `Isolated` — CouplingIndex < 10 e FanIn = 0 e FanOut = 0 (sem dependências registadas)
- **ArchitecturalRisk flag** — serviços `HubService` com tier `Critical` e `FanIn ≥ 5`
- **IsolationRisk flag** — serviços `Isolated` com tier `Critical` ou `Standard` (potencial governance gap)
- **Top hub services** (maior fan-in) e top acoplados (maior fan-out)
- **CouplingIndex médio do tenant** e **% de serviços Isolated**

**Orientado para Architect e Platform Admin** — suporta análise de blast radius, decisões de decomposição de serviços e identificação de single points of failure estruturais.

#### W.3 — GetAnomalyDetectionSummaryReport (OperationalIntelligence)

**Feature:** Sumário consolidado de todas as anomalias detetadas pelo NexTraceOne num período, por serviço. Responde à pergunta: "quais serviços têm múltiplos sinais de problema simultâneos?"

**Domínio:** Agrega anomalias de múltiplas fontes:
- `WasteSignal` (FinOps — idle/overprovision)
- `DriftFinding` (runtime — desvio de baseline)
- `SloObservation` com status `Breached`
- `ChaosExperiment` com status `Failed`
- `VulnerabilityAdvisoryRecord` com severity `Critical` ou `High`
- Incidentes correlacionados a mudanças recentes

**Capacidades:**
- Para cada serviço, lista todos os tipos de anomalia ativos no período com contagem
- **AnomalyCount total** por serviço (soma de todos os tipos)
- **AnomalyDensity tier:**
  - `Clean` — 0 anomalias ativas
  - `Moderate` — 1–2 tipos de anomalia
  - `Dense` — 3–4 tipos de anomalia
  - `Critical` — ≥ 5 tipos de anomalia simultâneos
- **MultiAnomalyServices** — lista de serviços com ≥ 3 tipos simultâneos (requerem atenção imediata)
- **Timeline de anomalias por dia** (30 pontos) — pico de anomalias por dia no tenant
- **Distribuição por tipo de anomalia** no tenant
- **Top serviços com maior AnomalyCount**

**Orientado para Tech Lead, Engineer e Platform Admin** — funciona como "early warning dashboard" unificado que cruza todos os sinais de alerta disponíveis, eliminando a necessidade de consultar cada relatório individualmente.

#### Configuração Wave W

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `changes.rollback_pattern.lookback_days` | 90 | 11440 | Período de análise para padrões de rollback |
| `changes.rollback_pattern.serial_threshold` | 4 | 11450 | Número mínimo de rollbacks para padrão Serial |
| `changes.rollback_pattern.max_services` | 50 | 11460 | Máximo de serviços no relatório de padrões |
| `catalog.coupling_index.max_services` | 100 | 11470 | Máximo de serviços no relatório de acoplamento |
| `catalog.coupling_index.hub_threshold` | 70 | 11480 | Threshold de CouplingIndex para HubService |
| `catalog.coupling_index.high_coupled_threshold` | 50 | 11490 | Threshold de CouplingIndex para HighlyCoupled |
| `runtime.anomaly_summary.lookback_days` | 30 | 11500 | Período de análise para sumário de anomalias |
| `runtime.anomaly_summary.max_services` | 100 | 11510 | Máximo de serviços no sumário de anomalias |

#### i18n Wave W

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `rollbackPattern.*` — padrões de rollback por serviço
- `serviceCouplingIndex.*` — índice de acoplamento entre serviços
- `anomalyDetectionSummary.*` — sumário consolidado de anomalias

**Totais estimados Wave W:** CG: ~916 testes (+13). Catalog: ~1924 testes (+13). OI: ~1240 testes (+16). Configuração: +8 config keys (sort 11440–11510). i18n: +3 secções (4 locales).

---

### Wave X — Frontend Intelligence & Contract Studio Visual Builders

**Objetivo:** Evoluir o frontend de superfície operacional para **plataforma de decisão inteligente por persona**, completar os visual builders para os novos tipos de contrato (GraphQL, Protobuf), e introduzir views executivas orientadas a KPIs consolidados. Esta wave fecha o gap entre a riqueza analítica do backend (Waves A–W) e a experiência de utilizador que a surfacia.

#### X.1 — Executive Intelligence Dashboard

**Feature:** Dashboard unificado para persona Executive/CTO com KPIs consolidados de todas as dimensões operacionais.

**Componentes:**
- **`ServiceHealthSummaryCard`** — score de saúde global do tenant: média ponderada de SLO compliance + risk center + ownership health + deployment success rate
- **`ChangeConfidenceGauge`** — gauge de confiança média de deployment nas últimas 4 semanas por tier de serviço
- **`ComplianceCoverageWidget`** — cobertura de compliance por standard (8 barras horizontais, % de serviços cobertos)
- **`FinOpsBudgetBurnWidget`** — % de budget consumido vs. período, com flag de burn rate acelerado
- **`TopRiskyServicesTable`** — top 5 serviços por score de risco com drill-down direto para Risk Center
- **`MttrTrendMiniChart`** — sparkline de MTTR dos últimos 30 dias para os 3 serviços mais críticos
- Todos os widgets ligados ao persona `Executive`; dados pré-filtrados por ownership relevante

**Orientado para Executive, CTO e Product** — responde a "o produto está a degradar ou a melhorar?" sem precisar navegar módulo a módulo.

#### X.2 — GraphQL & Protobuf Visual Studio no Contract Studio

**Feature:** Experiência visual de edição/comparação de schemas GraphQL SDL e Protobuf `.proto` dentro do Contract Studio existente, complementando os backends implementados em Waves G.3 e H.1.

**Componentes GraphQL:**
- **`GraphQlSchemaDiffViewer`** — diff side-by-side entre dois snapshots SDL com highlighting de breaking changes (vermelho), non-breaking additions (verde) e unchanged (cinzento)
- **`GraphQlSchemaExplorer`** — explorador de tipos, fields, queries, mutations e subscriptions com filtro e pesquisa
- **`GraphQlBreakingChangeBadge`** — badge inline na listagem de contratos quando existe breaking change entre a versão current e anterior
- Upload de ficheiro SDL ou textarea para `AnalyzeGraphQlSchema`

**Componentes Protobuf:**
- **`ProtobufSchemaDiffViewer`** — diff side-by-side de `.proto` com highlighting por tipo de change (message, field, service, rpc)
- **`ProtobufSchemaExplorer`** — explorador de messages, fields, services e RPCs com indicação de deprecated fields
- Upload de ficheiro `.proto` para `AnalyzeProtobufSchema`

**Orientado para Architect e Tech Lead** — fecha o loop entre análise de backend (Wave G.3/H.1) e experiência visual no Contract Studio.

#### X.3 — Persona-Aware Adaptive Navigation

**Feature:** Navegação adaptativa que reordena o menu e os quick actions com base na persona ativa do utilizador autenticado, tornando a experiência mais focada e reduzindo o cognitive load.

**Comportamentos por persona:**
- **Engineer** — destaca: Service Catalog (own services), Contracts (own team), Change Status, AI Assistant
- **Tech Lead** — destaca: Team Dashboard, Change Velocity, Operational Readiness, SLO Compliance
- **Architect** — destaca: Dependency Graph, Contract Adoption, API Exposure, Service Coupling Index
- **Platform Admin** — destaca: Risk Center, Compliance Coverage Matrix, EF Migrations Health, System Health
- **Executive** — destaca: Executive Dashboard, FinOps Burn, MTTR Trend, Compliance Overview
- **Auditor** — destaca: Audit Trail, Evidence Packs, Compliance Reports, Policy Compliance

**Implementação:** `PersonaNavigationConfig` no frontend (Zustand store) alimentado por `GET /api/v1/identity/me/persona-config` que retorna `quickActions[]` e `prioritizedModules[]` com base em roles + ownership.

**Orientado para todas as personas** — reforça o NexTraceOne como plataforma multi-persona e não apenas para Engineers.

#### Configuração Wave X

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `ui.executive_dashboard.enabled` | true | 11520 | Ativa o Executive Intelligence Dashboard |
| `ui.executive_dashboard.risk_threshold` | 60 | 11530 | Score mínimo de risco para destacar serviços no widget |
| `ui.contract_studio.graphql_diff.max_snapshot_age_days` | 90 | 11540 | Máximo de dias de antiguidade de snapshot para diff GraphQL |
| `ui.contract_studio.protobuf_diff.max_snapshot_age_days` | 90 | 11550 | Máximo de dias de antiguidade de snapshot para diff Protobuf |
| `ui.adaptive_navigation.enabled` | true | 11560 | Ativa navegação adaptativa por persona |
| `ui.adaptive_navigation.quick_actions_count` | 5 | 11570 | Número de quick actions em destaque por persona |
| `ui.executive_dashboard.finops_burn_warn_threshold_pct` | 80 | 11580 | % de budget consumido para flag de burn acelerado |
| `ui.executive_dashboard.mttr_sparkline_services` | 3 | 11590 | Número de serviços no sparkline de MTTR do Executive Dashboard |

#### i18n Wave X

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `executiveDashboard.*` — labels e títulos do Executive Intelligence Dashboard
- `graphqlDiffViewer.*` — interface do diff viewer de schemas GraphQL
- `protobufDiffViewer.*` — interface do diff viewer de schemas Protobuf
- `adaptiveNavigation.*` — quick actions e módulos priorizados por persona

**Totais estimados Wave X:** Frontend: ~45 novos componentes/pages. Configuração: +8 config keys (sort 11520–11590). i18n: +4 secções (4 locales). Backend: 1 novo endpoint `GET /api/v1/identity/me/persona-config`.

---

### Wave Y — AI Governance Deep Dive & Agentic Platform

**Objetivo:** Evoluir a capacidade de IA do NexTraceOne de "assistente com política" para **plataforma agentic governada**, com orquestração multi-step auditável, routing inteligente de modelos, redação contextual de PII/segredos nos prompts, e rastreabilidade completa de custo por equipa/caso de uso.

#### Y.1 — Agentic Runtime com Human-in-the-Loop

**Feature:** Execução de planos multi-step por agentes de IA, com pontos de aprovação humana configuráveis e auditoria completa de cada passo.

**Domínio:** Novo aggregate `AgentExecutionPlan` em `AIKnowledge` ou novo módulo `AIOrchestration`.

**Capacidades:**
- **`AgentExecutionPlan`** aggregate — plano com lista de `AgentStep` (nome, tipo, input, output, status, duração, custo estimado), `PlanStatus` (Pending/Running/WaitingApproval/Completed/Failed/Cancelled)
- **`AgentStepType`** enum — ContractLookup / IncidentCorrelation / DraftGeneration / RunbookProposal / AlertTriage / ExternalSearch
- **Human-in-the-loop gate** — passos com `RequiresApproval = true` pausam a execução até aprovação/rejeição via `ApproveAgentStep` command
- **Budget por plano** — plano define `MaxTokenBudget`; execução para automaticamente se orçamento excedido
- **`BlastRadiusThreshold` gate** — passos que envolvem serviços Critical com BlastRadius > threshold requerm aprovação automática
- **Audit trail completo** — cada passo registado com timestamp, modelo usado, tokens consumidos, custo, aprovador (se aplicável)
- 4 features: `SubmitAgentExecutionPlan`, `ApproveAgentStep`, `GetAgentPlanStatus`, `ListAgentExecutionHistory`
- Migration para tabelas `ai_agent_execution_plans` + `ai_agent_steps`

#### Y.2 — NLP-based Model Routing

**Feature:** Routing inteligente de prompts para o modelo mais adequado com base em análise semântica do intent, custo e latência esperados.

**Domínio:** Evolui o `RoutingDecision` existente (keyword heuristics) para embedding-based intent classification.

**Capacidades:**
- **Intent classifier leve** — embedding de prompt (via modelo local) → classificação em `PromptIntent` (CodeGeneration/DocumentSummarization/IncidentAnalysis/ContractDraft/ComplianceCheck/GeneralQuery)
- **`ModelRoutingPolicy`** entity — por `PromptIntent`: modelo preferido, fallback, max tokens, max cost per request, allowed tenants/groups
- **Routing resolution** — para cada prompt: classify intent → lookup policy → select model → apply budget check → execute
- **Cost-aware routing** — se modelo preferido excede budget restante do tenant, faz downgrade automático com nota na resposta
- **`GetModelRoutingDecisionLog`** query — auditoria de todas as decisões de routing com intent, modelo selecionado e motivo
- 3 config keys por intent (sort 11600+); i18n `modelRouting.*` em 4 locales; ~20 testes

#### Y.3 — AI Token Budget Attribution & Cost Transparency

**Feature:** Atribuição granular de custos de tokens de IA por equipa, serviço, caso de uso e modelo. Fecha o loop entre IA governada e FinOps contextual.

**Domínio:** Cruza `AgentQueryRecord` (Wave D.4) + `AgentExecutionPlan` (Wave Y.1) com `ServiceCostAllocationRecord` (Wave I.2) para IA.

**Capacidades:**
- **`AiTokenUsageRecord`** entity — `TenantId`, `TeamId`, `ServiceName`, `UseCase` (enum), `ModelId`, `InputTokens`, `OutputTokens`, `TotalTokens`, `EstimatedCostUsd`, `RequestedAt`
- **`GetAiTokenBudgetReport`** query — consumo de tokens por tenant, por equipa e por caso de uso no período. Inclui budget configurado vs. consumido, taxa de burn, top consumidores, top modelos
- **`GetAiCostAttributionReport`** query — custo estimado de IA distribuído por serviço/equipa, com `CostPerQuery` médio por tipo de uso
- **Budget enforcement** — `AiBudgetPolicy` por tenant/equipa: `MonthlyTokenBudget`, `AlertThresholdPct`, `BlockOnExhaustion`
- 4 config keys (sort 11630+); i18n `aiTokenBudget.*` + `aiCostAttribution.*` em 4 locales; ~18 testes

#### Configuração Wave Y

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `ai.agentic.max_steps_per_plan` | 10 | 11600 | Máximo de passos por plano de execução de agente |
| `ai.agentic.blast_radius_approval_threshold` | 10 | 11610 | Consumidores afetados acima deste valor requerem aprovação |
| `ai.agentic.default_token_budget_per_plan` | 50000 | 11620 | Budget padrão de tokens por plano de agente |
| `ai.routing.default_intent` | GeneralQuery | 11630 | Intent padrão quando classificação não é conclusiva |
| `ai.routing.cost_downgrade_enabled` | true | 11640 | Ativa downgrade de modelo por budget |
| `ai.budget.monthly_token_limit_default` | 1000000 | 11650 | Limite mensal padrão de tokens por tenant |
| `ai.budget.alert_threshold_pct` | 80 | 11660 | % de budget consumido para envio de alerta |
| `ai.budget.block_on_exhaustion` | false | 11670 | Bloqueia requests quando budget esgotado |

#### i18n Wave Y

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `agentExecutionPlan.*` — planos de execução agentic e aprovação humana
- `modelRouting.*` — decisões de routing de modelo por intent
- `aiTokenBudget.*` — orçamento e consumo de tokens por equipa/serviço
- `aiCostAttribution.*` — atribuição de custo de IA por serviço e caso de uso

**Totais estimados Wave Y:** AIKnowledge: ~50 testes novos. Configuração: +8 config keys (sort 11600–11670). i18n: +4 secções (4 locales). Novas migrations: 2 (AgentExecutionPlans + AiTokenUsageRecords).

---

### Wave Z — Integration Ecosystem Completion

**Objetivo:** Completar as integrações de ecosistema que permitem ao NexTraceOne ser **autónomo na ingestão** de eventos externos e ser **consumível programaticamente** por ferramentas de automação — fechando os últimos gaps de integração identificados nas secções 3.2, 3.3, 3.4 e 11.2 do documento.

#### Z.1 — Kafka / Message Queue Consumer Real

**Feature:** Worker process dedicado para consumo real de eventos de filas externas (Kafka, RabbitMQ, Azure Service Bus, AWS SQS). Converte eventos externos no modelo canónico do NexTraceOne sem intervenção manual.

**Domínio:** `NexTraceOne.Worker.EventConsumer` — processo `IHostedService` separado, configurável por tenant.

**Capacidades:**
- **`EventConsumerWorker`** — `BackgroundService` que lê de fila configurada (tipo, connection string, topic/queue) e normaliza eventos para `Release`, `IncidentEvent` ou `ExternalChangeRequest` via estratégia de mapeamento
- **`IEventNormalizationStrategy`** abstraction — strategies por source type: `KafkaChangeEventStrategy`, `ServiceBusChangeEventStrategy`, `SqsChangeEventStrategy`, `RabbitMqChangeEventStrategy`
- **Dead letter queue** — eventos que falham normalização após 3 tentativas vão para `EventConsumerDeadLetterRecord` (tabela auditável)
- **Monitoring endpoint** `GET /api/v1/integrations/event-consumer/status` — estado de cada consumer (running/paused/error), throughput, last event timestamp, dead letter count
- Config por tenant: `queue.consumer.type`, `queue.consumer.connection_string`, `queue.consumer.topic`, `queue.consumer.enabled`
- ~25 testes de normalização e routing

#### Z.2 — SDK NexTrace (`nexone` CLI + NuGet + npm)

**Feature:** SDK público para integração programática com o NexTraceOne. Inclui CLI `nexone`, biblioteca NuGet para .NET e pacote npm para Node.js/scripts de pipeline.

**Componentes:**
- **CLI `nexone`** — `nexone service describe <name>`, `nexone contract diff <id>`, `nexone change status <sha>`, `nexone confidence score <release-id>`, `nexone compliance check --standard GDPR`
- **`NexTrace.Sdk` (NuGet)** — cliente tipado para todos os módulos: `ServiceCatalogClient`, `ContractClient`, `ChangeClient`, `ComplianceClient`. Autenticação via `PlatformApiToken` (Wave D.4). Retry policies via Polly.
- **`nexone-sdk` (npm)** — wrapper TypeScript sobre as mesmas APIs. Útil para GitHub Actions, GitLab CI e scripts de pipeline.
- **GitHub Action oficial** `nexone/change-confidence-gate@v1` — action que consulta o score de confiança de uma release e falha o pipeline se abaixo de threshold configurável
- **Versionamento semântico** — SDK versiona independentemente do produto; breaking changes requerem major bump; changelog automático
- Documentação em `docs/sdk/` com exemplos por caso de uso (CI/CD integration, contract validation, compliance check)

#### Z.3 — ClickHouse Analytics Provider

**Feature:** Provider alternativo de storage analítico baseado em ClickHouse para workloads de observabilidade e telemetria de alto volume. Complementa o Elasticsearch como opção para clientes com requisitos de performance analítica extrema.

**Domínio:** Novo adapter `NexTraceOne.Infrastructure.ClickHouse` no padrão provider existente.

**Capacidades:**
- **`IClickHouseAnalyticsWriter`** — interface para ingestão batch de telemetria (traces, logs, métricas)
- **`IClickHouseAnalyticsReader`** — interface para queries analíticas (P50/P95/P99, aggregations por serviço/ambiente/período)
- **Schema ClickHouse** — tabela `traces` (MergeTree, partition by toYYYYMM(timestamp)), `metrics` (SummingMergeTree), `logs` (ReplicatedMergeTree com TTL)
- **`ClickHouseHealthCheck`** — health check integrado no `GET /api/v1/system/health`
- Configuração via `appsettings`: `Analytics:Provider = "ClickHouse"` | `"Elasticsearch"` | `"InMemory"` (para testes)
- Documentação de setup em `docs/observability/clickhouse-setup.md` com docker-compose snippet

#### Configuração Wave Z

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `integrations.event_consumer.max_concurrent_consumers` | 4 | 11680 | Máximo de consumidores paralelos por tenant |
| `integrations.event_consumer.dead_letter_max_attempts` | 3 | 11690 | Tentativas antes de mover para dead letter queue |
| `integrations.event_consumer.processing_timeout_seconds` | 30 | 11700 | Timeout por evento antes de dead letter |
| `sdk.platform_api.rate_limit_per_minute` | 300 | 11710 | Rate limit por token da Platform API |
| `sdk.platform_api.max_page_size` | 100 | 11720 | Tamanho máximo de página nas queries do SDK |
| `analytics.clickhouse.batch_size` | 1000 | 11730 | Tamanho do batch de ingestão para ClickHouse |
| `analytics.clickhouse.flush_interval_seconds` | 5 | 11740 | Intervalo de flush do buffer de ingestão |
| `analytics.clickhouse.default_ttl_days` | 90 | 11750 | TTL padrão dos dados analíticos em ClickHouse |

#### i18n Wave Z

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `eventConsumer.*` — estado e monitorização do event consumer worker
- `sdkIntegration.*` — documentação inline e tooltips de configuração do SDK
- `clickhouseProvider.*` — configuração e estado do provider ClickHouse

**Totais estimados Wave Z:** Worker: ~25 testes. SDK: documentação + 2 packages. Configuração: +8 config keys (sort 11680–11750). i18n: +3 secções (4 locales). Novas migrations: 1 (EventConsumerDeadLetterRecords).

---

### Wave AA — V3 Frontend Evolution (Dashboards + Persona Suites + Source-of-Truth Surfaces)

**Estado:** 📘 PLANEADA — documento detalhado em [`docs/V3-EVOLUTION-FRONTEND-DASHBOARDS.md`](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md).

**Objetivo:** elevar o frontend do NexTraceOne de "app de módulos" para **Operational Intelligence Surface persona-aware** e os Custom Dashboards para **Governed Intelligence Boards**, consolidando simultaneamente as superfícies que materializam o NexTraceOne como **Source of Truth** operacional. Wave AA é a contraparte frontend das waves backend A–Z — transforma os ~46 features analytics/governance já entregues (waves A–R) e os 24 planeados (S–Z) em **superfícies de decisão enterprise** coesas.

**Escopo (12 waves incrementais):**

- **V3.1 Dashboard Intelligence Foundation** — versionamento de dashboards (`DashboardRevision`), variables/tokens (`$service`, `$env`, `$team`, `$timeRange`), `SharingPolicy` granular (Private/Team/Tenant/PublicLink × Read/Edit), auditoria completa.
- **V3.2 Query-driven Widgets & Widget SDK** — `QueryWidget` com NQL governada, `IWidgetKind` como contrato interno estável, Annotations API (changes/incidents/deploys).
- **V3.3 Live, Cross-filter, Drill-down** — SSE live streaming, cross-filter contextual, drill-down para páginas-dono ("Open with…"), deep-linking estável.
- **V3.4 AI-assisted Dashboard Creation & Notebook Mode** — AI Agent "Dashboard Composer" sob governança, Notebooks (cells Markdown/Query/Widget/Action/AI) para investigação e post-mortems.
- **V3.5 Frontend Platform Uplift** — Design Tokens v2, Command Palette global, View Transitions, WCAG 2.2 AA, performance budgets em CI, Storybook, telemetry de UX.
- **V3.6 Governance, Reports & Embedding** — Scheduled snapshots (PDF/PNG/email via SMTP), public signed links + iframe embed, Dashboard-as-Code (YAML canonicalizado), policies sobre dashboards, lifecycle Draft/Published/Deprecated/Archived.
- **V3.7 Real-time Collaboration & War Room** — presença, cursores partilhados, edição concorrente via CRDT (Yjs MIT), comentários ancorados, modo War Room.
- **V3.8 Marketplace, Plugin SDK & Widgets de Terceiros** — Template Gallery interno, Plugin SDK público via Module Federation, governance de plugins assinados.
- **V3.9 Advanced NQL + Alerting from Widget + Mobile On-Call** — NQL avançado governado (joins/subqueries/UDFs whitelisted), criar monitor/ação a partir de widget, PWA on-call com offline cache.
- **V3.10 Persona-first Experience Suites** — 7 homes dedicadas (Engineer Cockpit, Tech Lead Team Command Center, Architect Landscape View, Product Portfolio Home, Executive Brief Center, Platform Admin Cockpit, Auditor Console); `IPersonaHomeResolver` + `IUserOperationalScopeResolver` + Persona Quick Actions Registry; persona switcher governado.
- **V3.11 Source-of-Truth Consolidation Surfaces** — 11 Centers que materializam as waves backend: Compliance Scorecard Center (G/H/I/J/K/L), Risk Center UX (F.2/N.2), FinOps Context Views (I.2/O.2), Change Confidence Hub, Release Calendar & Window Gate (F.1/S.1), Rollback Intelligence Cockpit (J.3), Blast Radius Explorer (Q.3), Evidence Pack Viewer (N.3 + SLSA), Operational Readiness Board (L.3), Drift Center (M.3/Q.1), SLO Service Center + Chaos Lab + Post-Incident Learning Board (J.2/N.1/S.3 + K.1/V.2 + T.1). `ISourceOfTruthNavigationGraph` para deep-links canônicos e integração com AI Assistant.
- **V3.12 Contract Studio Visual + AI Agent Marketplace + IDE Console + Admin Consoles** — 7 sub-épicos: (12.1) Visual Designers para REST/OpenAPI + AsyncAPI 3.x + SOAP/WSDL + GraphQL + Protobuf + Unified Publication Center; (12.2) AI Agent Registry/Builder + AI Token & Budget Governance Console + External AI Integration Console; (12.3) IDE Extensions Admin para Visual Studio e VS Code + Developer Enrollment UX; (12.4) Break Glass + JIT Privileged Access + Delegated Access + Access Review Workflows; (12.5) Licensing & Entitlements Admin (online + offline/air-gapped); (12.6) Knowledge Hub ↔ Notebooks ↔ Runbooks Bridge; (12.7) Dashboards-as-Code GitOps Console.

**Alinhamento com visão oficial do produto (Copilot Instructions v3):**

- **§6 Personas** — V3.10 entrega segmentação real por persona (não só por cargo); considera papel funcional + escopo organizacional + responsabilidade operacional + permissões reais + ownership.
- **§7 Módulos oficiais** — V3.11 e V3.12 cobrem Foundation (Identity/Org/Teams/Environments/Integrations/Licensing), Services (Catalog/Reliability/Lifecycle), Contracts (todos os tipos + Studio), Changes (Calendar/Gate/Rollback/Blast Radius/Evidence), Operations (Incidents/Runbooks/Drift/Chaos/SLO), Knowledge (Hub/Notebooks/Runbooks), AI (Registry/Agents/Policies/IDE), Governance (Reports/Risk/Compliance/FinOps/Policy).
- **§§8/9/10** — Contratos e mudanças são first-class (V3.12.1 + V3.11.4/5/6/7); observabilidade é meio, não fim (V3.11 contextualiza tudo em serviço/contrato/mudança/ambiente).
- **§11 IA governada** — V3.12.2 materializa model registry, quotas, budget, audit completo, policies por tenant/ambiente/persona.
- **§12 IDE Extensions** — V3.12.3 entrega a experiência IDE como capacidade de primeira classe, não acessório.
- **§13 FinOps contextual** — V3.11.3 entrega FinOps *contextual*, nunca genérico.
- **§§16/17 Segurança/Licensing** — V3.12.4 (Break Glass/JIT/Delegated/Access Reviews) e V3.12.5 (License online + offline).
- **§18 UI enterprise sóbria** — V3.5 reforça design tokens, WCAG, performance budgets.

**Critérios de aceite agregados:**
- Persona fit ≥70% (% de utilizadores que usam Persona Home como entrada principal).
- Source of Truth adoption ≥80% (decisões de promoção/rollback tomadas via Change Confidence Hub).
- Compliance time-to-report <10 minutos por standard.
- Contract Studio adoption ≥50% dos contratos novos.
- AI governance ≥85% das invocações via agentes registrados.
- Break Glass safety 100% (gravação + audit + expiração).

**Volumetria estimada Wave AA:** dashboards core ~100 testes (V3.1–V3.4); frontend uplift + Storybook ≥75 componentes (V3.5); governance/reports ≥50 testes (V3.6); collaboration CRDT ≥30 testes (V3.7); marketplace ≥40 testes (V3.8); NQL+Alerting+PWA ≥60 testes (V3.9); **Persona Suites ≥40 testes (V3.10); 11 Centers ≥60 testes (V3.11); Contract Studio + AI Agents + IDE + Admin ≥70 testes (V3.12)** — total indicativo ≥525 testes dedicados, além da cobertura já existente nos módulos backend.

**Referências cruzadas:** ver documento detalhado [`docs/V3-EVOLUTION-FRONTEND-DASHBOARDS.md`](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md) secções 3 (plano por wave), 5 (ordem recomendada e trilhos paralelos A–G), 6 (indicadores de sucesso), 7 (riscos e mitigações) e 9 (referências no código).

---

### Wave AB — Knowledge Graph & Semantic Relations

**Objetivo:** Materializar o NexTraceOne como Knowledge Hub operacional através de um grafo semântico de relações entre entidades (serviços, contratos, runbooks, incidentes, mudanças, equipas), rastreabilidade de linhagem de contratos por versão, e agregação de aprendizado operacional a partir do histórico de incidentes e runbooks.

#### AB.1 — GetKnowledgeRelationGraph (Catalog)

**Feature:** Grafo de relações semânticas entre entidades do NexTraceOne — responde "o que está ligado a este serviço/contrato/incidente?" com um grafo navegável de adjacências ponderadas.

**Domínio:** Cruza `ServiceAsset`, `ApiAsset`, `ServiceDependency`, `IncidentEvent`, `Release`, `Runbook` e `OperationalNote` para construir um grafo de adjacência ponderado.

**Capacidades:**
- **Nós do grafo** (`KnowledgeNode`) — entidades tipificadas: `Service`, `Contract`, `Runbook`, `Incident`, `Release`, `Team`, `OperationalNote`
- **Arestas do grafo** (`KnowledgeEdge`) — relações nomeadas: `OwnedBy`, `DependsOn`, `PublishesContract`, `ConsumesContract`, `CorrelatedWith`, `MitigatedBy`, `DocumentedIn`, `DeployedAs`
- **Subgrafo por âncora** — dado um serviço ou contrato âncora, retorna o grafo de profundidade configurável (1–3 saltos)
- **RelationStrength** — peso de cada aresta baseado em frequência de correlação e recência (decaimento por `relation_strength_decay_days`)
- **Filtro por tipo de nó e relação** — permite focar apenas em contratos, runbooks ou incidentes
- **KnowledgeGraphSummary** — contagens de nós/arestas, tipos mais frequentes, densidade do grafo no tenant
- Retorno em formato compatível com visualização de grafo (adjacency list + node metadata)

**Orientado para Architect, Tech Lead e Engineer** — permite navegar o contexto operacional de qualquer entidade sem saltar entre módulos.

#### AB.2 — GetContractLineageReport (Catalog)

**Feature:** Linhagem de versões de um contrato — rastreabilidade completa de quem criou, aprovou, modificou e descontinuou cada versão, com métricas de longevidade e estabilidade.

**Domínio:** Agrega `ContractChangelog`, `ContractVersion`, `ApprovalRecord` e eventos de `ReleaseTimeline` que referenciaram o contrato.

**Capacidades:**
- **LineageNode por versão** — versão, estado de ciclo de vida, autor da mudança, aprovador, data de promoção e data de deprecation
- **BreakingChangeCount por transição de versão** — número de breaking changes entre versões consecutivas
- **ConsumerImpactAtDeprecation** — número de consumidores ativos no momento em que a versão foi deprecada
- **VersionRetentionDays** — tempo que cada versão permaneceu ativa antes de ser substituída
- **StabilityScore de linhagem** — inversamente proporcional a `BreakingChangeCount / TotalTransitions`
- **LineageSummary** — total de versões, versão mais longeva, versão mais curta, autor com mais contribuições
- Filtro por protocolo, equipa e período de publicação

**Orientado para Architect e Auditor** — suporta compliance de governança de API e análise de maturidade do processo de versionamento ao longo do tempo.

#### AB.3 — GetIncidentKnowledgeBaseReport (OperationalIntelligence)

**Feature:** Agregação de aprendizado operacional a partir do histórico de incidentes, runbooks e post-mortems. Responde "o que aprendemos com os incidentes passados?" de forma quantificável.

**Domínio:** Cruza `IncidentEvent`, `Runbook`, `OperationalNote` e `ProposedRunbook` (Wave B.4) por serviço e tipo de incidente.

**Capacidades:**
- Para cada categoria de incidente (por `IncidentType` ou tag), agrega:
  - Frequência histórica e tendência (aumentando/diminuindo)
  - **ResolutionConfidence** — % de incidentes do tipo com runbook aprovado disponível
  - **MeanTimeToRunbook** — tempo médio entre abertura do incidente e aplicação de runbook
  - **RunbookEffectivenessScore** — % de incidentes em que o runbook foi aplicado e o incidente foi fechado sem reabertura
- **KnowledgeGap flag** — tipos de incidente recorrentes sem runbook aprovado
- **StaleRunbook flag** — runbooks não revistos em > `stale_runbook_days` com incidentes ativos associados
- **Top 10 tipos de incidente com maior frequência e menor ResolutionConfidence**
- **KnowledgeMaturityScore global** — média ponderada de ResolutionConfidence + RunbookEffectivenessScore por serviço

**Orientado para Tech Lead, Platform Admin e Engineer** — fecha o loop entre incidentes operacionais e o Knowledge Hub, transformando histórico em aprendizado governado e mensurável.

#### Configuração Wave AB

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `catalog.knowledge_graph.max_depth` | 2 | 11760 | Profundidade máxima do subgrafo de relações semânticas |
| `catalog.knowledge_graph.max_nodes` | 200 | 11770 | Máximo de nós retornados no grafo de conhecimento |
| `catalog.knowledge_graph.relation_strength_decay_days` | 90 | 11780 | Dias de decaimento de força de relação por inatividade |
| `catalog.contract_lineage.max_versions` | 50 | 11790 | Máximo de versões na linhagem de um contrato |
| `catalog.contract_lineage.lookback_days` | 365 | 11800 | Período de análise para linhagem de contratos |
| `runtime.incident_knowledge.stale_runbook_days` | 180 | 11810 | Dias sem revisão para classificar runbook como Stale |
| `runtime.incident_knowledge.lookback_days` | 365 | 11820 | Período de análise para base de conhecimento de incidentes |
| `runtime.incident_knowledge.top_incidents` | 10 | 11830 | Número de tipos de incidente no top ranking |

#### i18n Wave AB

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `knowledgeRelationGraph.*` — grafo de relações semânticas entre entidades do NexTraceOne
- `contractLineage.*` — linhagem e rastreabilidade de versões de contratos
- `incidentKnowledgeBase.*` — base de conhecimento e aprendizado operacional de incidentes

**Totais estimados Wave AB:** Catalog: ~30 testes (AB.1 ~14 + AB.2 ~16). OI: ~14 testes (AB.3). Configuração: +8 config keys (sort 11760–11830). i18n: +3 secções (4 locales). **Wave AB PLANEADA**.

---

### Wave AC — Self-Service & Platform Adoption Intelligence

**Objetivo:** Introduzir métricas de adoção e saúde de onboarding que respondam "as equipas estão a usar o NexTraceOne eficazmente?" — habilitando Platform Admins a medir o valor real da plataforma, identificar equipas que precisam de suporte para adotar capacidades críticas, e quantificar o ROI da adoção por equipa.

#### AC.1 — GetOnboardingHealthReport (Catalog)

**Feature:** Scorecard de completude de onboarding por equipa e serviço. Identifica serviços subregistados ou equipas com baixa adoção de capacidades core do NexTraceOne.

**Domínio:** Cruza `ServiceAsset`, `ApiAsset`, `TeamOwnership`, `Runbook`, `SloObservation` e `ProfilingSession` para medir completude de onboarding por dimensão.

**Capacidades:**
- Para cada serviço no tenant, calcula `OnboardingScore` (0–100) por dimensões ponderadas:
  - **Ownership** (20%) — serviço tem equipa proprietária + ownership não obsoleto
  - **Contracts** (25%) — ≥1 contrato em estado `Approved` ou `Locked` registado
  - **Runbook** (20%) — ≥1 runbook aprovado associado ao serviço
  - **SLO** (20%) — ≥1 SLO observado nos últimos 30 dias
  - **Profiling** (15%) — ≥1 sessão de profiling nos últimos 90 dias
- **OnboardingTier por serviço:**
  - `Complete` — score ≥ 90
  - `Advanced` — score ≥ 70
  - `Basic` — score ≥ 40
  - `Minimal` — score < 40 (candidates para suporte prioritário)
- **Top serviços com menor OnboardingScore**
- **Distribuição por OnboardingTier** no tenant
- **TeamOnboardingAvg** — score médio por equipa
- **TenantOnboardingScore** — média global ponderada por tier de serviço (Critical > Standard > Experimental)

**Orientado para Platform Admin e Executive** — responde "qual é a adoção real da plataforma?" sem assumir que registar o serviço equivale a usar as capacidades.

#### AC.2 — GetDeveloperActivityReport (IdentityAccess)

**Feature:** Relatório de atividade de desenvolvedores no NexTraceOne por equipa e período. Identifica equipas mais ativas, champions internos e gaps de participação para decisões de enablement.

**Domínio:** Agrega eventos de auditoria (`AuditTrailEntry`) filtrados por ações de criação/edição de entidades de produto (contratos, runbooks, notas, releases).

**Capacidades:**
- Para cada utilizador/equipa, calcula no período:
  - `ContractsCreated` e `ContractsUpdated`
  - `RunbooksCreated` e `RunbooksUpdated`
  - `ReleasesRegistered`
  - `OperationalNotesCreated`
  - `TotalActions` — soma ponderada de todas as ações rastreadas (contratos=3, runbooks=2, resto=1)
- **ActivityTier por utilizador:**
  - `HighlyActive` — TotalActions ≥ percentil 75 do tenant
  - `Active` — TotalActions ≥ percentil 50
  - `Occasional` — TotalActions ≥ 1
  - `Inactive` — TotalActions = 0 (potencial gap de enablement)
- **TeamActivityScore** — soma ponderada de ações por equipa
- **Top 10 utilizadores mais ativos** e **top 10 equipas mais ativas**
- **InactiveTeams** — equipas sem nenhuma ação no período (candidatas a outreach de adoção)
- Filtro por equipa, tipo de ação e período (7–90 dias)

**Orientado para Platform Admin e Tech Lead** — suporta decisões de enablement, formação e identificação de champions internos da plataforma.

#### AC.3 — GetPlatformAdoptionReport (OperationalIntelligence)

**Feature:** Relatório de adoção de capacidades da plataforma por equipa. Mede que percentagem das capacidades disponíveis cada equipa está a usar ativamente.

**Domínio:** Agrega sinais de uso de features por equipa: SLO tracking, chaos experiments, profiling, compliance reports, AI assistant, change confidence, release calendar.

**Capacidades:**
- Para cada equipa, verifica adoção de 7 capacidades core:
  - `SloTracking` — ≥1 SLO observado nos últimos 30 dias
  - `ChaosEngineering` — ≥1 experimento nos últimos 90 dias
  - `ContinuousProfiling` — ≥1 sessão de profiling nos últimos 90 dias
  - `ComplianceReports` — ≥1 relatório de compliance gerado nos últimos 90 dias
  - `ChangeConfidence` — ≥1 avaliação de confiança por release nos últimos 30 dias
  - `ReleaseCalendar` — ≥1 janela de deploy registada nos últimos 90 dias
  - `AiAssistant` — ≥1 consulta ao AI assistant nos últimos 30 dias
- **AdoptionScore por equipa** — % de capacidades usadas (0–100%)
- **AdoptionTier:** `Pioneer` ≥80% / `Adopter` ≥60% / `Explorer` ≥40% / `Laggard` <40%
- **CapabilityAdoptionRate global** — % de equipas que usam cada capacidade (identifica capacidades subutilizadas)
- **GrowthOpportunity** — capacidades com menos de 30% de adoção global (targets para enablement)
- Filtro por equipa e período de análise

**Orientado para Platform Admin e Executive** — responde "a plataforma está a entregar valor?" com dados reais de uso, não métricas de registo.

#### Configuração Wave AC

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `catalog.onboarding_health.complete_threshold` | 90 | 11840 | Score mínimo para OnboardingTier Complete |
| `catalog.onboarding_health.advanced_threshold` | 70 | 11850 | Score mínimo para OnboardingTier Advanced |
| `catalog.onboarding_health.basic_threshold` | 40 | 11860 | Score mínimo para OnboardingTier Basic |
| `audit.developer_activity.lookback_days` | 30 | 11870 | Período padrão para relatório de atividade de developer |
| `audit.developer_activity.highly_active_percentile` | 75 | 11880 | Percentil de TotalActions para ActivityTier HighlyActive |
| `platform.adoption.slo_lookback_days` | 30 | 11890 | Lookback para verificação de adoção de SLO Tracking |
| `platform.adoption.feature_lookback_days` | 90 | 11900 | Lookback padrão para verificação de features de adoption |
| `platform.adoption.pioneer_threshold` | 80 | 11910 | Threshold (%) para AdoptionTier Pioneer |

#### i18n Wave AC

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `onboardingHealth.*` — saúde e completude de onboarding por serviço e equipa
- `developerActivity.*` — atividade de desenvolvedores e equipas na plataforma
- `platformAdoption.*` — adoção de capacidades da plataforma por equipa

**Totais estimados Wave AC:** Catalog: ~13 testes (AC.1). IA/Audit: ~12 testes (AC.2). OI: ~14 testes (AC.3). Configuração: +8 config keys (sort 11840–11910). i18n: +3 secções (4 locales). **Wave AC PLANEADA**.

---

### Wave AD — Zero Trust & Security Posture Analytics

**Objetivo:** Introduzir visibilidade de postura de segurança no contexto de serviços e contratos — respondendo "quão segura é a comunicação entre serviços?", "existem riscos de exposição de segredos nos artefactos governados?" e "há padrões anómalos de acesso à plataforma?" — reforçando o NexTraceOne como plataforma de confiança operacional, não apenas de catálogo.

#### AD.1 — GetZeroTrustPostureReport (ChangeGovernance)

**Feature:** Avaliação de postura Zero Trust por serviço: cobertura de autenticação mútua, rotação de tokens e uso de políticas de acesso explícitas. Identifica serviços expostos sem controlo de comunicação adequado.

**Domínio:** Cruza metadados de segurança de `ServiceAsset` (campos `AuthenticationScheme`, `MtlsEnabled`, `TokenRotationPolicy`) com `PolicyDefinition` (Wave D.3) e security gates de `ReleaseTimeline`.

**Capacidades:**
- Para cada serviço, calcula `ZeroTrustScore` (0–100) por dimensões ponderadas:
  - **Authentication** (30%) — esquema de autenticação definido e ativo (Bearer/mTLS/API Key)
  - **Mutual TLS** (25%) — mTLS habilitado para comunicação inter-serviço
  - **Token Rotation** (20%) — política de rotação de tokens definida e não expirada
  - **Policy Coverage** (25%) — ≥1 `PolicyDefinition` de acesso aplicada ao serviço
- **ZeroTrustTier por serviço:**
  - `Enforced` — score ≥ 85 (postura robusta)
  - `Controlled` — score ≥ 65 (postura adequada com gaps menores)
  - `Partial` — score ≥ 40 (gaps significativos)
  - `Exposed` — score < 40 (risco elevado — comunicação sem controlo adequado)
- **CriticalExposure flag** — serviços de tier `Critical` com `ZeroTrustTier = Exposed`
- **Distribuição por ZeroTrustTier** no tenant
- **Top serviços com menor score** (prioritários para hardening)
- **TenantZeroTrustScore** — média ponderada por tier de serviço

**Orientado para Security, Platform Admin e Architect** — introduz visibilidade de segurança no contexto do catálogo de serviços, não como ferramenta isolada de security scanning.

#### AD.2 — GetSecretsExposureRiskReport (Catalog)

**Feature:** Deteção de risco de exposição de segredos em artefactos governados pelo NexTraceOne — contratos, notas operacionais e runbooks — usando pattern matching leve sem dependências externas.

**Domínio:** Analisa conteúdo textual de `ApiAsset` (exemplos de payloads, descrições), `OperationalNote` e `Runbook` por padrões suspeitos de segredos.

**Capacidades:**
- **Pattern detection leve** — varredura por expressões regulares para padrões comuns:
  - API keys (padrões `sk-`, `AKIA`, `ghp_`, etc.)
  - Tokens JWT expostos em exemplos de payload
  - Connection strings com credenciais embebidas (`password=`, `pwd=`, `secret=`)
  - IPs privados em exemplos de payload de produção
  - Emails pessoais em payloads de exemplo
- **ExposureRisk por artefacto:** `None / Low / Medium / High / Critical`
- **AffectedArtifacts list** — lista de contratos, runbooks e notas com ExposureRisk ≥ Medium
- **ExposureCategory distribution** — distribuição por tipo de pattern detectado
- **Top serviços com maior número de artefactos em risco**
- **AuditTrail** — cada deteção registada com tipo de artefacto, campo, pattern e timestamp
- Configurável: quais patterns ativar e thresholds de risco por tipo de pattern

**Orientado para Security, Auditor e Platform Admin** — previne exposição acidental de segredos via conteúdo dos contratos e runbooks, um vetor frequentemente ignorado em ferramentas de API governance.

#### AD.3 — GetAccessPatternAnomalyReport (IdentityAccess)

**Feature:** Deteção de padrões anómalos de acesso ao NexTraceOne — acessos incomuns a contratos sensíveis, picos de consultas fora do padrão, ou primeiro acesso a recursos restritos por utilizadores não habituais.

**Domínio:** Analisa `AuditTrailEntry` para identificar desvios em padrões de acesso por utilizador, equipa e recurso comparados com baseline histórica.

**Capacidades:**
- **BaselineAccess por utilizador** — padrão de acesso histórico (dias da semana, horas, tipos de recurso, volume médio)
- **AnomalySignal** por tipo:
  - `OffHours` — acesso fora de horário habitual (definido por percentil histórico do utilizador)
  - `VolumetricSpike` — volume de queries > `volumetric_spike_multiplier`× a média do utilizador
  - `FirstAccessSensitive` — primeiro acesso a contrato marcado como `Restricted` ou `Partner`
  - `UnusualResource` — acesso a tipo de recurso nunca acedido pelo utilizador no histórico
  - `BulkExport` — download/export de > `bulk_export_threshold` contratos em sessão curta
- **RiskScore por evento anómalo** — combinação de tipo de anomalia + sensibilidade do recurso
- **AnomalyDensityByUser** — utilizadores com > 3 sinais no período (risco de insider threat ou comprometimento de conta)
- **Top recursos mais acedidos anomalamente** (contratos, runbooks, relatórios)
- Filtro por severidade, tipo de anomalia e período

**Orientado para Auditor, Security e Platform Admin** — complementa o audit trail passivo com deteção ativa de padrões suspeitos, aproximando o NexTraceOne de UEBA leve para o seu ecossistema.

#### Configuração Wave AD

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `security.zero_trust.enforced_threshold` | 85 | 11920 | Score mínimo para classificação ZeroTrustTier Enforced |
| `security.zero_trust.critical_exposure_threshold` | 40 | 11930 | Score máximo para classificação ZeroTrustTier Exposed |
| `catalog.secrets_exposure.max_artifacts` | 500 | 11940 | Máximo de artefactos inspecionados por relatório |
| `catalog.secrets_exposure.min_risk_level` | Medium | 11950 | Nível mínimo de risco para inclusão no relatório |
| `audit.access_anomaly.lookback_days` | 30 | 11960 | Período de análise para padrões de acesso |
| `audit.access_anomaly.volumetric_spike_multiplier` | 3 | 11970 | Multiplicador de volume para flag VolumetricSpike |
| `audit.access_anomaly.bulk_export_threshold` | 20 | 11980 | Número de contratos para flag BulkExport |
| `audit.access_anomaly.max_users` | 100 | 11990 | Máximo de utilizadores no relatório de anomalias de acesso |

#### i18n Wave AD

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `zeroTrustPosture.*` — postura Zero Trust e cobertura de autenticação mútua por serviço
- `secretsExposureRisk.*` — risco de exposição de segredos em artefactos governados
- `accessPatternAnomaly.*` — anomalias de padrão de acesso e sinais de risco comportamental

**Totais estimados Wave AD:** CG: ~13 testes (AD.1). Catalog: ~13 testes (AD.2). IA/Audit: ~14 testes (AD.3). Configuração: +8 config keys (sort 11920–11990). i18n: +3 secções (4 locales). Nota: AD.2 requer apenas regex scanning leve, sem dependências externas de SAST. **Wave AD PLANEADA**.

---

### Wave AE — Contract Testing & API Backward Compatibility

**Objetivo:** Completar o ciclo de governance de contratos introduzindo visibilidade de cobertura de testes de contrato (consumer-driven contract testing), avaliação de impacto downstream de breaking changes, e scorecard de compatibilidade retroativa — transformando o NexTraceOne na fonte única de confiança para evolução segura de APIs.

#### AE.1 — GetContractTestCoverageReport + IngestContractTestResult (Catalog)

**Feature:** Cobertura de testes de contrato por serviço e API. Identifica serviços sem testes de contrato registados, onde uma mudança pode quebrar consumidores silenciosamente.

**Domínio:** Novo aggregate `ContractTestRecord` — registo de resultados de testes de contrato ingeridos via webhook ou API (de Pact Broker, pipeline CI ou ferramenta de contract testing).

**Capacidades:**
- **`ContractTestRecord`** entity — `ApiAssetId`, `ConsumerServiceName`, `ProducerServiceName`, `TestSuite`, `TestStatus` (`Passed/Failed/Pending`), `FailureReason`, `TestedAt`, `PactBrokerUrl` (opcional), `CiPipelineRef`
- **`IngestContractTestResult`** command — endpoint de ingestão de resultados via pipeline CI/CD
- **`GetContractTestCoverageReport`** query — cobertura por serviço:
  - `TestedApiCount` / `TotalApiCount` → `CoverageRate`
  - `TestPassRate` — % de testes passing no período
  - `FailedContracts` — lista de contratos com testes failing
  - **CoverageTier:** `Full` ≥90% / `Good` ≥70% / `Partial` ≥40% / `None` <40%
  - **Top serviços com menor cobertura** e **top contratos com testes failing**
  - **UncoveredConsumerPairs** — pares produtor-consumidor sem teste registado
- Migration para tabela `contract_test_records`

**Orientado para Engineer, Tech Lead e Architect** — fecha o gap entre "contrato documentado" e "contrato testado", tornando o NexTraceOne o ponto central de confiança para evolução de APIs.

#### AE.2 — GetSchemaBreakingChangeImpactReport (Catalog)

**Feature:** Avaliação de impacto downstream quando é detetada uma breaking change num schema de contrato. Responde "se eu fizer esta breaking change, quem é afetado e qual o risco real?"

**Domínio:** Cruza `ContractChangelog` (breaking changes registadas) com `ConsumerExpectation` (Wave Q.2) e `ServiceDependency` para propagar o impacto transitivo.

**Capacidades:**
- Dado um `ApiAssetId` + `ChangelogEntryId` com `IsBreaking = true`, calcula:
  - **DirectConsumers** — serviços com `ConsumerExpectation` registada para esta API
  - **IndirectConsumers** — serviços que dependem dos `DirectConsumers` até `max_hop_depth` saltos
  - **ImpactScore** — ponderado por tier dos consumidores afetados (Critical=3, Standard=2, Experimental=1)
  - **TotalAffectedServices** — soma de diretos + indiretos únicos
  - **MitigationOptions** — sugestões geradas: deprecar antes de remover, manter compat via versioning, notificar consumidores
- **BreakingChangeImpactTier:**
  - `Contained` — apenas consumidores `Experimental` afetados
  - `Moderate` — ≥1 consumidor `Standard` afetado
  - `Significant` — ≥1 consumidor `Critical` afetado
  - `Widespread` — ≥5 serviços afetados (diretos + indiretos)
- **ByEnvironment breakdown** — impacto diferenciado por ambiente (production vs. non-prod consumers)

**Orientado para Architect, Tech Lead e Engineer** — transforma a deteção de breaking changes (Waves G.3/H.1) em decisão informada com quantificação de impacto real por tier e ambiente.

#### AE.3 — GetApiBackwardCompatibilityReport (Catalog)

**Feature:** Scorecard de compatibilidade retroativa por contrato ao longo do tempo. Mede quão "safe to evolve" é cada contrato para os seus consumidores, com perspetiva longitudinal.

**Domínio:** Analisa o histórico de `ContractChangelog` por contrato: rácio de breaking vs. non-breaking changes, cadência de versões major e adoção pelos consumidores.

**Capacidades:**
- Para cada contrato com histórico de changelogs, calcula:
  - `BreakingChangeRate` — % de changelogs com `IsBreaking = true` no período
  - `MajorVersionCount` — número de versões major no período
  - `ConsumerAdoptionLag` — dias médios para consumidores migrarem para a versão mais recente (via `ConsumerExpectation`)
  - `BackwardCompatibilityScore` = `(1 - BreakingChangeRate) * 100` ajustado por `ConsumerAdoptionLag`
- **CompatibilityTier por contrato:**
  - `Stable` — score ≥ 85, BreakingChangeRate < 10%
  - `Evolving` — score ≥ 65, breaking changes controladas com adoção rápida
  - `Volatile` — score ≥ 40, breaking changes frequentes ou adoção lenta
  - `Unstable` — score < 40 (breaking changes frequentes + consumidores sem migrar)
- **StagnationFlag** — contratos `Stable` com zero changelogs em > `stagnation_days` (possível abandono)
- **Top contratos mais estáveis** e **top contratos mais voláteis**
- **TenantCompatibilityIndex** — média ponderada de BackwardCompatibilityScore no tenant

**Orientado para Architect, Tech Lead e Platform Admin** — perspetiva longitudinal da qualidade de evolução do catálogo de contratos, complementando o snapshot de saúde atual (Waves M.1/O.1/R.2).

#### Configuração Wave AE

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `contracts.test_coverage.full_threshold` | 90 | 12000 | Threshold (%) para CoverageTier Full de testes de contrato |
| `contracts.test_coverage.good_threshold` | 70 | 12010 | Threshold (%) para CoverageTier Good de testes de contrato |
| `contracts.test_coverage.max_services` | 100 | 12020 | Máximo de serviços no relatório de cobertura de testes |
| `contracts.breaking_change_impact.max_hop_depth` | 2 | 12030 | Profundidade máxima de propagação de impacto transitivo |
| `contracts.breaking_change_impact.max_consumers` | 200 | 12040 | Máximo de consumidores analisados por breaking change |
| `contracts.backward_compat.stable_threshold` | 85 | 12050 | Score mínimo para CompatibilityTier Stable |
| `contracts.backward_compat.stagnation_days` | 180 | 12060 | Dias sem changelog para flag StagnationFlag |
| `contracts.backward_compat.max_contracts` | 200 | 12070 | Máximo de contratos no relatório de compatibilidade retroativa |

#### i18n Wave AE

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `contractTestCoverage.*` — cobertura de testes de contrato por serviço e pares produtor-consumidor
- `schemaBreakingChangeImpact.*` — impacto de breaking changes em consumidores diretos e indiretos
- `apiBackwardCompatibility.*` — scorecard de compatibilidade retroativa e evolução de contratos

**Totais estimados Wave AE:** Catalog: ~42 testes (AE.1 ~16 + AE.2 ~12 + AE.3 ~14). Configuração: +8 config keys (sort 12000–12070). i18n: +3 secções (4 locales). Novas migrations: 1 (`ContractTestRecords`). **Wave AE PLANEADA**.

---

### Wave AF — Service Lifecycle Governance

**Objetivo:** Materializar o NexTraceOne como controlador de ciclo de vida de serviços — acompanhando transições de estado, avaliando condições de retirada segura e rastreando o progresso de migração de consumidores para longe de serviços deprecados. Esta wave fecha o gap entre "catálogo de serviços" e "governança de fim de vida de serviços".

#### AF.1 — GetServiceLifecycleTransitionReport (Catalog)

**Feature:** Relatório de transições de estado no ciclo de vida dos serviços. Identifica serviços estagnados num estado, serviços a transitar mais rapidamente que o esperado, e serviços com transições bloqueadas.

**Domínio:** Analisa histórico de mudanças de `ServiceLifecycleState` (Active/Deprecated/Sunset/Retired) por serviço via `ServiceAsset` + `ContractChangelog` como proxy de actividade.

**Capacidades:**
- Para cada serviço, calcula:
  - `CurrentLifecycleState` — estado atual (Active/Deprecated/Sunset/Retired)
  - `DaysInCurrentState` — dias no estado atual
  - `LastTransitionDate` — data da última transição de estado
  - `TransitionCount` — número de transições no período
- **StagnationFlag** — serviços `Deprecated` há mais de `stagnation_days` sem progresso de migração de consumidores (identificado via `ConsumerExpectation`)
- **AcceleratedRetirementFlag** — serviços passaram de `Active` para `Sunset` em menos de `min_deprecation_days` (possível violação de processo de governance)
- **BlockedTransitionFlag** — serviços `Deprecated` com `ConsumerExpectation` de consumidores `Critical` ainda ativos (retirada bloqueada por dependência)
- **LifecycleDistribution** — distribuição de serviços por estado de ciclo de vida no tenant
- **Top serviços com `DaysInCurrentState` mais alto** (por estado) — identifica stagnation por estado
- Filtro por equipa, tier de serviço e estado de ciclo de vida

**Orientado para Architect, Platform Admin e Tech Lead** — suporta decisões de when e how retirar serviços de forma segura, sem quebrar consumidores.

#### AF.2 — GetServiceRetirementReadinessReport (Catalog)

**Feature:** Avaliação de prontidão para retirar um serviço específico. Responde "posso deprecar/retirar este serviço com segurança?" com score composto e lista de bloqueadores.

**Domínio:** Para um dado `ServiceAssetId`, agrega sinais de múltiplas fontes para determinar se a retirada é segura.

**Capacidades:**
- **RetirementReadinessScore** (0–100) calculado por dimensões ponderadas:
  - **ConsumerMigrated** (40%) — % de consumidores com `ConsumerExpectation` migrados para versão mais recente ou para alternativa
  - **ContractsDeprecated** (25%) — % de contratos do serviço em estado `Deprecated` ou `Sunset` (não `Active`/`Approved`)
  - **RunbookDocumented** (15%) — runbook de decommission existente e aprovado
  - **DependantsNotified** (20%) — % de equipas consumidoras notificadas (via `OperationalNote` ou canal configurado)
- **RetirementReadinessTier:**
  - `Ready` — score ≥ 85 (pode ser retirado)
  - `NearReady` — score ≥ 65 (gaps menores)
  - `Blocked` — score ≥ 40 (bloqueadores significativos)
  - `NotReady` — score < 40 (retirada prematura)
- **BlockerList** — lista explícita de o que impede a retirada (consumidores ainda activos, contratos ainda Active, runbook ausente)
- **MigrationProgress** — para cada consumidor ativo, estado de migração para alternativa
- Endpoint específico por serviço: `GET /api/v1/catalog/services/{id}/retirement-readiness`

**Orientado para Architect e Platform Admin** — transforma a decisão de retirada de serviço de ad-hoc para processo estruturado e auditável.

#### AF.3 — GetServiceMigrationProgressReport (Catalog)

**Feature:** Rastreamento de progresso de migração de consumidores de um serviço `Deprecated` ou `Sunset` para a alternativa designada. Responde "quantos consumidores ainda estão presos no serviço antigo?"

**Domínio:** Cruza `ServiceDependency` + `ConsumerExpectation` + `ApiAsset` (alternativa designada via `ReplacedByServiceId` ou `SuccessorContractId`) para construir pipeline de migração.

**Capacidades:**
- Para cada serviço `Deprecated` ou `Sunset` com alternativa designada:
  - **TotalConsumers** — número total de consumidores com dependência registada
  - **MigratedConsumers** — consumidores sem `ConsumerExpectation` ativa no serviço antigo e com dependência no serviço novo
  - **InProgressConsumers** — consumidores com dependências em ambos (em transição)
  - **StuckConsumers** — consumidores sem qualquer sinal de migração no período
- **MigrationCompletionRate** — `MigratedConsumers / TotalConsumers * 100`
- **MigrationTier:** `Complete` ≥100% / `Advanced` ≥75% / `InProgress` ≥25% / `Lagging` <25%
- **EstimatedCompletionDate** — projeção baseada na taxa de migração atual (linear)
- **Top consumidores bloqueados** — consumidores `Stuck` com tier `Critical` ou `Standard`
- **DailyMigrationTimeline** — série temporal de 30 dias de progresso

**Orientado para Tech Lead, Architect e Platform Admin** — suporta o processo de sunset controlado de serviços com visibilidade de progresso de migração por equipa consumidora.

#### Configuração Wave AF

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `catalog.lifecycle.stagnation_days` | 90 | 12080 | Dias no estado Deprecated sem progresso para flag StagnationFlag |
| `catalog.lifecycle.min_deprecation_days` | 30 | 12090 | Dias mínimos de estado Deprecated antes de Sunset (AcceleratedRetirementFlag) |
| `catalog.lifecycle.max_services` | 200 | 12100 | Máximo de serviços no relatório de transições de ciclo de vida |
| `catalog.retirement_readiness.ready_threshold` | 85 | 12110 | Score mínimo para RetirementReadinessTier Ready |
| `catalog.retirement_readiness.near_ready_threshold` | 65 | 12120 | Score mínimo para RetirementReadinessTier NearReady |
| `catalog.migration_progress.lookback_days` | 90 | 12130 | Período de análise para progresso de migração |
| `catalog.migration_progress.stuck_threshold_days` | 30 | 12140 | Dias sem sinal de migração para classificar como StuckConsumer |
| `catalog.migration_progress.max_services` | 100 | 12150 | Máximo de serviços no relatório de progresso de migração |

#### i18n Wave AF

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `serviceLifecycleTransition.*` — transições de estado no ciclo de vida de serviços
- `serviceRetirementReadiness.*` — prontidão para retirada de serviços e lista de bloqueadores
- `serviceMigrationProgress.*` — progresso de migração de consumidores de serviços deprecated

**Totais estimados Wave AF:** Catalog: ~42 testes (AF.1 ~13 + AF.2 ~15 + AF.3 ~14). Configuração: +8 config keys (sort 12080–12150). i18n: +3 secções (4 locales). **Wave AF PLANEADA**.

---

### Wave AG — FinOps Advanced Attribution

**Objetivo:** Aprofundar a capacidade FinOps contextual do NexTraceOne — passando de "custo por serviço" para atribuição granular por ambiente, por release e por padrão de desperdício operacional. Esta wave fecha o gap entre observabilidade de custo e decisão de rightsizing informada por contexto operacional real.

#### AG.1 — GetEnvironmentCostComparisonReport (OperationalIntelligence)

**Feature:** Comparação de custo operacional entre ambientes (dev/staging/prod) por serviço. Identifica serviços onde o custo em não-produção excede de forma desproporcional o custo em produção — sinal de desperdício em ambientes de teste.

**Domínio:** Agrega `ServiceCostAllocationRecord` por serviço e ambiente no período, com ratio não-prod/prod.

**Capacidades:**
- Para cada serviço com custo registado em múltiplos ambientes, calcula:
  - `ProdCostUsd` — custo total em ambiente `Production`
  - `NonProdCostUsd` — custo total em ambientes não-produção (soma)
  - `NonProdToProdRatio` — `NonProdCostUsd / ProdCostUsd`
  - `NonProdWasteCostUsd` — custo excedente não-prod vs. prod (se ratio > `expected_ratio`)
- **EnvironmentEfficiencyTier por serviço:**
  - `Optimal` — ratio ≤ 0.5 (não-prod custa metade ou menos que prod — esperado)
  - `Acceptable` — ratio ≤ 1.0
  - `Overprovisioned` — ratio ≤ 2.0 (não-prod custa mais que prod)
  - `WasteAlert` — ratio > 2.0 (non-prod custa 2× ou mais que prod)
- **TotalNonProdWasteUsd** — waste estimado total do tenant em não-produção
- **Top serviços com maior WasteCostUsd**
- **DistributionByTier** — distribuição de serviços por EnvironmentEfficiencyTier
- Filtro por equipa e período (30–90 dias)

**Orientado para FinOps, Platform Admin e Tech Lead** — quantifica desperdício em ambientes de teste que frequentemente é invisível nos relatórios de custo cloud genéricos.

#### AG.2 — GetCostPerReleaseReport (OperationalIntelligence)

**Feature:** Custo operacional atribuído por release de serviço. Responde "qual é o custo de deploy de uma release?" e identifica releases com cost spike pós-deploy.

**Domínio:** Junta `Release` (data de deploy, serviço, ambiente) com `ServiceCostAllocationRecord` para calcular custo médio diário nos N dias após cada release versus baseline pré-release.

**Capacidades:**
- Para cada release no período, calcula:
  - `PreReleaseDailyAvgCostUsd` — custo médio diário nos `pre_release_days` antes do deploy
  - `PostReleaseDailyAvgCostUsd` — custo médio diário nos `post_release_days` após o deploy
  - `CostDeltaPct` — variação percentual post vs. pre
  - `PostReleaseTotalCostUsd` — custo total no período de análise pós-deploy
- **CostImpactTier por release:**
  - `Neutral` — delta entre -10% e +10%
  - `CostSaving` — delta < -10% (release reduziu custo — eficiência ou scale-down)
  - `MinorIncrease` — delta 10–30%
  - `MajorIncrease` — delta 30–100%
  - `CostSpike` — delta > 100% (release causou spike de custo — regressão ou over-provisioning)
- **Top releases com maior CostDeltaPct** (positivo e negativo)
- **ReleaseCostSummary** — média de custo por release no tenant, % com CostSpike, % com CostSaving
- Correlação com `DeploymentStatus` (Failed/RolledBack com CostSpike = flag especial `WastedDeploymentCost`)

**Orientado para FinOps, Architect e Platform Admin** — conecta releases (Wave P.3) com custo real pós-deploy, habilitando optimização de deployment strategies.

#### AG.3 — GetFinOpsWasteAnalysisReport (OperationalIntelligence)

**Feature:** Análise consolidada de desperdício operacional por serviço, cruzando múltiplos sinais de waste: idle resources, over-provisioning, failed deployments, e custo de não-prod desproporcionado.

**Domínio:** Agrega sinais de `ServiceCostAllocationRecord` + `WasteSignal` (existente) + `Release` (deployments failed/rolled back) + análise de `EnvironmentEfficiencyTier` (AG.1).

**Capacidades:**
- Para cada serviço, identifica categorias de waste:
  - **IdleWaste** — custo de serviços `LowLoad` (Wave U.3) com custo acima da mediana
  - **OverProvisioningWaste** — custo de não-prod desproporcional (ratio > `expected_ratio`)
  - **FailedDeploymentWaste** — custo acumulado de releases `Failed` ou `RolledBack`
  - **DriftWaste** — custo de serviços com `DriftFinding.Severity = High/Critical` (degradação não resolvida aumenta custo)
- **WasteScore por serviço** (0–100) — soma ponderada de categorias de waste
- **WasteTier:** `Clean` ≤10 / `Minor` ≤30 / `Significant` ≤60 / `Critical` >60
- **TotalEstimatedWasteUsd** — estimativa total de desperdício no tenant no período
- **WasteByCategory** — distribuição percentual por categoria
- **Top serviços com maior WasteScore** e maior `EstimatedWasteUsd`
- **WasteOpportunity** — total estimado de poupança se top 10 serviços forem otimizados

**Orientado para FinOps, Platform Admin e Executive** — visão executiva do desperdício total da plataforma, com breakdown acionável por categoria e serviço.

#### Configuração Wave AG

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `finops.environment_cost.expected_nonprod_ratio` | 0.5 | 12160 | Ratio non-prod/prod esperado para EnvironmentEfficiencyTier Optimal |
| `finops.environment_cost.lookback_days` | 30 | 12170 | Período de análise para comparação de custo entre ambientes |
| `finops.cost_per_release.pre_release_days` | 7 | 12180 | Dias antes do deploy para baseline de custo |
| `finops.cost_per_release.post_release_days` | 7 | 12190 | Dias após o deploy para análise de custo pós-release |
| `finops.cost_per_release.spike_threshold_pct` | 100 | 12200 | Threshold (%) de CostDeltaPct para classificação CostSpike |
| `finops.waste_analysis.lookback_days` | 30 | 12210 | Período de análise para relatório de waste |
| `finops.waste_analysis.max_services` | 100 | 12220 | Máximo de serviços no relatório de waste |
| `finops.waste_analysis.significant_waste_threshold` | 30 | 12230 | Threshold de WasteScore para WasteTier Significant |

#### i18n Wave AG

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `environmentCostComparison.*` — comparação de custo entre ambientes e detecção de waste em não-prod
- `costPerRelease.*` — custo operacional por release e identificação de cost spikes pós-deploy
- `finOpsWasteAnalysis.*` — análise consolidada de desperdício por categoria e serviço

**Totais estimados Wave AG:** OI: ~43 testes (AG.1 ~13 + AG.2 ~15 + AG.3 ~15). Configuração: +8 config keys (sort 12160–12230). i18n: +3 secções (4 locales). **Wave AG PLANEADA**.

---

### Wave AH — Event-Driven Architecture Governance

**Objetivo:** Introduzir governança de arquitecturas event-driven — rastreando evolução de schemas de eventos, detectando desequilíbrios entre produtores e consumidores de eventos, e validando conformidade dos produtores com os contratos AsyncAPI registados. Esta wave completa o ciclo de governance de contratos estendendo-o ao domínio de eventos.

#### AH.1 — GetEventSchemaEvolutionReport (Catalog)

**Feature:** Rastreamento da evolução de schemas de eventos (AsyncAPI/Kafka) ao longo do tempo. Identifica eventos com historiais de breaking changes frequentes e consumidores que ainda utilizam versões obsoletas de schemas.

**Domínio:** Agrega `ContractChangelog` filtrado por contratos AsyncAPI + `ConsumerExpectation` por schema de evento + `DetectGraphQlBreakingChanges` como referência de padrão para deteção de breaking changes em AsyncAPI.

**Capacidades:**
- Para cada contrato AsyncAPI/evento no período, calcula:
  - `TotalSchemaChanges` — total de changelogs registados
  - `BreakingSchemaChanges` — changelogs com `IsBreaking = true`
  - `BreakingChangeRate` — % de mudanças breaking
  - `ActiveConsumersOnOldVersion` — consumidores com `ConsumerExpectation` em versão anterior à current
  - `SchemaLagDays` — diferença em dias entre data de publicação da versão atual e data de última atualização do consumidor mais atrasado
- **EventSchemaStabilityTier:** `Stable` (BreakingChangeRate < 5%) / `Evolving` (5–20%) / `Volatile` (20–50%) / `Unstable` (>50%)
- **MigrationLag flag** — contratos com `ActiveConsumersOnOldVersion > 0` e `SchemaLagDays > lag_alert_days`
- **Top eventos com maior BreakingChangeRate** e maior `SchemaLagDays`
- **TenantEventSchemaHealthSummary** — distribuição de contratos AsyncAPI por tier de estabilidade

**Orientado para Architect e Tech Lead** — suporta governança de event-driven architecture com visibilidade de maturidade de evolução de schemas, complementando a análise de contratos REST/GraphQL/Protobuf.

#### AH.2 — GetEventProducerConsumerBalanceReport (Catalog)

**Feature:** Análise de equilíbrio entre produtores e consumidores de eventos registados. Identifica eventos órfãos (produzidos sem consumidores), consumidores sem produtor registado (dependência cega) e eventos sobrecarregados (muitos consumidores num único produtor).

**Domínio:** Cruza `ApiAsset` de tipo `AsyncApiEvent` com `ServiceDependency` + `ConsumerExpectation` para mapear o grafo de produção/consumo de eventos.

**Capacidades:**
- Para cada contrato de evento (AsyncAPI/Kafka) no tenant:
  - **ProducerCount** — número de serviços que produzem este evento
  - **ConsumerCount** — número de serviços que consomem este evento
  - **IsOrphaned** — `ConsumerCount = 0` e contrato `Active` (evento produzido sem utilidade registada)
  - **IsBlind** — `ProducerCount = 0` e `ConsumerCount > 0` (consumidores dependem de evento sem produtor registado)
  - **FanOutRisk** — `ConsumerCount ≥ fan_out_threshold` (evento com muitos consumidores — blast radius elevado em breaking change)
- **OrphanedEvents** — lista de contratos `Active` sem consumidores (candidatos a deprecação)
- **BlindConsumers** — lista de consumidores sem produtor registado (gap de catalogação)
- **HighFanOutEvents** — eventos com `FanOutRisk = true` ordenados por ConsumerCount
- **BalanceSummary** — % de eventos orphaned, % com blind consumers, % com FanOutRisk, total de eventos no tenant

**Orientado para Architect e Platform Admin** — identifica problemas estruturais na arquitectura de eventos que não são visíveis sem um catálogo de contratos centralizado.

#### AH.3 — GetEventContractComplianceReport (Catalog)

**Feature:** Conformidade dos produtores de eventos com os contratos AsyncAPI registados. Responde "os eventos produzidos em runtime estão em conformidade com o schema registado?"

**Domínio:** Cruza `ApiAsset` (AsyncAPI contrato) com `RuntimeSnapshot` (eventos observados via telemetria) e `ContractTestRecord` (Wave AE.1 — testes de contrato de eventos) para calcular compliance.

**Capacidades:**
- Para cada contrato de evento com produtores e dados de runtime:
  - **SchemaComplianceRate** — % de eventos observados que passaram validação de schema (via `ContractTestRecord` ou runtime sampling)
  - **PayloadViolationCount** — número de payloads que violaram o schema registado no período
  - **UnregisteredFields** — campos presentes nos payloads mas não no schema (extensão não documentada)
  - **MissingRequiredFields** — campos obrigatórios ausentes em algum payload
- **ComplianceTier por contrato:**
  - `Compliant` — SchemaComplianceRate ≥ 99%
  - `MinorViolations` — SchemaComplianceRate ≥ 95%
  - `Degraded` — SchemaComplianceRate ≥ 80%
  - `NonCompliant` — SchemaComplianceRate < 80%
- **TenantEventComplianceScore** — média ponderada de SchemaComplianceRate por contrato
- **Top contratos não-conformes** e **top produtores com maior PayloadViolationCount**
- **ViolationTimeline** — série temporal de 30 dias de violações por tipo

**Orientado para Architect, Engineer e Auditor** — fecha o loop entre o contrato AsyncAPI registado e a realidade em runtime, tornando o NexTraceOne a fonte de verdade para conformidade de eventos.

#### Configuração Wave AH

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `catalog.event_schema.lag_alert_days` | 30 | 12240 | Dias de SchemaLag para flag MigrationLag em esquemas de eventos |
| `catalog.event_schema.max_contracts` | 200 | 12250 | Máximo de contratos AsyncAPI no relatório de evolução |
| `catalog.event_balance.fan_out_threshold` | 10 | 12260 | Número de consumidores para flag FanOutRisk |
| `catalog.event_balance.max_contracts` | 200 | 12270 | Máximo de contratos no relatório de equilíbrio produtor-consumidor |
| `catalog.event_compliance.lookback_days` | 30 | 12280 | Período de análise para conformidade de contratos de eventos |
| `catalog.event_compliance.compliant_threshold` | 99 | 12290 | SchemaComplianceRate mínimo para ComplianceTier Compliant |
| `catalog.event_compliance.degraded_threshold` | 80 | 12300 | SchemaComplianceRate mínimo para ComplianceTier Degraded |
| `catalog.event_compliance.max_contracts` | 200 | 12310 | Máximo de contratos no relatório de conformidade de eventos |

#### i18n Wave AH

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `eventSchemaEvolution.*` — evolução de schemas de eventos AsyncAPI e migração de consumidores
- `eventProducerConsumerBalance.*` — equilíbrio produtor-consumidor e deteção de eventos órfãos
- `eventContractCompliance.*` — conformidade de produtores de eventos com contratos AsyncAPI

**Totais estimados Wave AH:** Catalog: ~42 testes (AH.1 ~13 + AH.2 ~14 + AH.3 ~15). Configuração: +8 config keys (sort 12240–12310). i18n: +3 secções (4 locales). **Wave AH PLANEADA**.

---

### Wave AI — Predictive Intelligence & Forecasting

**Objetivo:** Introduzir capacidades de inteligência preditiva no NexTraceOne — passando de relatórios descritivos ("o que aconteceu?") para análises prescritivas ("o que vai acontecer?"). Esta wave usa o histórico operacional acumulado pelas waves anteriores para produzir forecasts de risco de deployment, projeções de capacidade e scoring de probabilidade de incidente — sempre governados, auditáveis e contextualizados por serviço.

#### AI.1 — GetDeploymentRiskForecastReport (ChangeGovernance)

**Feature:** Scoring preditivo de risco para deployments futuros ou em curso, baseado em padrões históricos do serviço, contexto de ambiente e sinais operacionais recentes. Responde "qual é o risco desta release específica antes de ela chegar a produção?"

**Domínio:** Combina `Release` (histórico de deployments), `ServiceRiskProfile` (Wave F.2), `ChangeConfidenceBreakdown`, `RollbackPatternReport` (Wave W.1) e `EnvironmentStabilityReport` (Wave T.3) para um modelo de scoring heurístico sem ML externo.

**Capacidades:**
- Para cada release em estado `Pending` ou recente (`Running/Succeeded`), calcula `ForecastRiskScore` (0–100) por dimensões:
  - **HistoricalRollbackRate do serviço** (25%) — se o serviço tem padrão `Serial` de rollback, score aumenta
  - **CurrentEnvironmentInstability** (20%) — se o ambiente de destino está `Unstable` ou `Critical` (Wave T.3)
  - **ServiceRiskProfileScore** (20%) — score atual do Risk Center (Wave F.2)
  - **ChangeConfidenceInverse** (20%) — 100 - confidence score atual da release
  - **RecentIncidentRate** (15%) — taxa de incidentes pós-deploy nos últimos 30 dias para este serviço
- **RiskForecastTier:**
  - `Low` — score < 25
  - `Moderate` — score < 50
  - `High` — score < 75
  - `Critical` — score ≥ 75 (recomendação automática de aprovação adicional ou delay)
- **ForecastExplanation** — lista das dimensões que mais contribuíram para o score (top 3 fatores)
- **RecommendedActions** — sugestões baseadas nos fatores dominantes (ex: "resolver drift aberto antes do deploy", "aguardar janela de mudança agendada")
- **Top releases pendentes de alto risco** no tenant
- Endpoint: `GET /api/v1/changes/releases/{id}/risk-forecast`

**Orientado para Engineer, Tech Lead e Platform Admin** — suporte proativo à decisão de promover ou adiar uma release, antes do incidente acontecer.

#### AI.2 — GetCapacityTrendForecastReport (OperationalIntelligence)

**Feature:** Projeção de tendências de capacidade (throughput, latência, custo) por serviço baseada em extrapolação linear do histórico de `RuntimeSnapshot`. Identifica serviços que vão aproximar-se de thresholds críticos nos próximos N dias.

**Domínio:** Analisa séries temporais de `RuntimeSnapshot` (AvgLatencyMs, AvgThroughput, AvgErrorRate) por serviço e ambiente, aplicando regressão linear simples para projeção.

**Capacidades:**
- Para cada serviço com ≥ `min_data_points` snapshots no histórico, calcula:
  - **ThroughputTrend** — slope da regressão linear de `AvgThroughput` (positivo = crescimento)
  - **LatencyTrend** — slope da regressão de `AvgLatencyMs` (positivo = degradação)
  - **ErrorRateTrend** — slope da regressão de `AvgErrorRate`
  - **ProjectedThroughputIn30Days** e **ProjectedLatencyIn30Days** — extrapolação dos valores atuais
  - **DaysToLatencyThreshold** — dias estimados até `AvgLatencyMs > latency_critical_threshold` (se tendência ascendente)
  - **DaysToErrorRateThreshold** — dias estimados até `AvgErrorRate > error_rate_critical_threshold`
- **ForecastAlertTier:**
  - `Stable` — nenhum threshold vai ser atingido em 90 dias com tendência atual
  - `WatchList` — threshold estimado em 31–90 dias
  - `AtRisk` — threshold estimado em 8–30 dias
  - `Imminent` — threshold estimado em ≤ 7 dias (ação imediata recomendada)
- **Top serviços com menor `DaysToThreshold`** (mais urgentes)
- **TenantCapacitySummary** — % de serviços por ForecastAlertTier
- Filtro por ambiente e tier de serviço

**Orientado para Architect, Platform Admin e Engineer** — transforma o histórico de observabilidade em decisão proativa de rightsizing e escalamento, antes de SLOs serem violados.

#### AI.3 — GetIncidentProbabilityReport (OperationalIntelligence)

**Feature:** Scoring de probabilidade de incidente por serviço nas próximas 48–72 horas, baseado na convergência de sinais de risco operacional: drifts abertos, chaos coverage gaps, SLO em degradação, e mudanças recentes de alto risco.

**Domínio:** Agrega sinais de `DriftFinding`, `SloObservation`, `ChaosExperiment`, `Release` recentes e `VulnerabilityAdvisoryRecord` para calcular um score de probabilidade heurístico.

**Capacidades:**
- Para cada serviço ativo, calcula `IncidentProbabilityScore` (0–100) por contribuições:
  - **OpenDriftSignals** (25%) — número de `DriftFinding` abertos com severidade High/Critical
  - **SloBreachTrend** (25%) — % de SLO observations em estado `Breached` nas últimas 72h
  - **ChaosGap** (20%) — se serviço Critical sem `FullCoverage` de chaos (Wave V.2)
  - **RecentHighRiskRelease** (20%) — se existe release com `ForecastRiskScore > 75` nas últimas 24h (Wave AI.1)
  - **OpenVulnerabilities** (10%) — número de `VulnerabilityAdvisoryRecord` severity Critical/High
- **IncidentProbabilityTier:**
  - `Unlikely` — score < 20
  - `Possible` — score < 40
  - `Probable` — score < 65
  - `Imminent` — score ≥ 65 (alerta proativo recomendado)
- **ProbabilityExplanation** — top 3 fatores contribuintes (para cada serviço)
- **TenantRiskHeatmap** — distribuição de serviços por tier e top 10 serviços mais em risco
- **AlertServicesList** — serviços `Imminent` com drill-down para cada sinal contribuinte
- Refresh recomendado: periódico (via Quartz.NET job) com caching de `incident_probability_cache_ttl_minutes`

**Orientado para Engineer, Tech Lead e Platform Admin** — funciona como "early warning system" unificado que combina todos os sinais de risco numa única visão proativa, eliminando a necessidade de monitorar cada relatório individualmente.

#### Configuração Wave AI

| Key | Default | Sort | Descrição |
|-----|---------|------|-----------|
| `changes.risk_forecast.critical_threshold` | 75 | 12320 | Score mínimo de ForecastRiskScore para tier Critical |
| `changes.risk_forecast.high_threshold` | 50 | 12330 | Score mínimo para tier High |
| `runtime.capacity_forecast.min_data_points` | 14 | 12340 | Mínimo de snapshots para habilitar projeção de capacidade |
| `runtime.capacity_forecast.latency_critical_ms` | 2000 | 12350 | Threshold de latência (ms) para alerta de capacidade |
| `runtime.capacity_forecast.error_rate_critical_pct` | 5 | 12360 | Threshold de error rate (%) para alerta de capacidade |
| `runtime.incident_probability.imminent_threshold` | 65 | 12370 | Score mínimo para IncidentProbabilityTier Imminent |
| `runtime.incident_probability.probable_threshold` | 40 | 12380 | Score mínimo para IncidentProbabilityTier Probable |
| `runtime.incident_probability.cache_ttl_minutes` | 15 | 12390 | TTL do cache de scoring de probabilidade de incidente |

#### i18n Wave AI

Secções adicionadas em **4 locales** (en, pt-BR, pt-PT, es):
- `deploymentRiskForecast.*` — previsão de risco de deployment e recomendações proativas
- `capacityTrendForecast.*` — projeção de capacidade e alertas de threshold por serviço
- `incidentProbability.*` — scoring de probabilidade de incidente e early warning por serviço

**Totais estimados Wave AI:** CG: ~13 testes (AI.1). OI: ~28 testes (AI.2 ~13 + AI.3 ~15). Configuração: +8 config keys (sort 12320–12390). i18n: +3 secções (4 locales). 1 novo endpoint backend: `GET /api/v1/changes/releases/{id}/risk-forecast`. Job Quartz.NET para refresh periódico de probabilidade de incidente. **Wave AI PLANEADA**.

---

### Priorização recomendada das Waves

Respeita a "Ordem recomendada de priorização do produto" (capítulo 26 das Copilot Instructions):

1. ✅ **Wave A completo** — A.1 Change Intelligence preditivo + A.2 Data Contracts + Consumer Inventory + A.3 Service Tier + Ownership + A.4 AI Evaluation Harness + A.5 ML correlation + A.6 FinOps + Cost-aware gate. **COMPLETO**.
2. ✅ **Wave B completo** — B.1 AsyncAPI 3.x parity + B.2 CLI (nex catalog describe, nex change status) + PlatformApiToken + B.3 Backstage bridge + ExternalChangeRequest + OTel recipe + B.4 Knowledge Freshness + ProposedRunbook. **COMPLETO**.
3. ✅ **Wave C.1 (supply-chain) + C.2 (Evidence integrity + DORA + Access Review escalation)** — destrancam clientes enterprise regulados. **PARCIALMENTE COMPLETO** (C.3 eBPF e C.4 Helm pendentes — Wave D).
4. 🔲 **Wave C.4 (K8s operator + air-gapped)** — destranca escala e mercado defesa/finance.
5. ✅ **Wave D.1 (Digital Twin — What-if + Topology Snapshot) + D.1.b (Failure Simulation) + D.4 (Agent-to-Agent Protocol) + D.2 (Benchmarks anonimizados) + D.3 (Policy Studio)** — Todas as sub-waves D implementadas. **WAVE D COMPLETO**.
6. ✅ **Wave D.2** — `TenantBenchmarkConsent` + `BenchmarkSnapshotRecord` + DORA percentil cross-tenant anonimizado. ChangeGovernance: 607 testes.
7. ✅ **Wave D.3** — `PolicyDefinition` + engine de avaliação JSON puro (sem OPA) + Policy Studio CQRS. IdentityAccess: 550 testes.
8. ✅ **Wave E** — `ProfilingSession` (Continuous Profiling) + `GetMigrationHealthReport`. OI: 992 testes. **WAVE E COMPLETO**.
9. ✅ **Wave F.1** — `ReleaseCalendarEntry` (Release Calendar) — janelas de deploy/freeze/hotfix/maintenance com `IsChangeWindowOpen`. CG: 642 testes.
10. ✅ **Wave F.2** — `ServiceRiskProfile` (Risk Center) — score de risco ponderado por 4 dimensões + relatório ranqueado para Exec/Platform Admin. **WAVE F COMPLETO**.
11. ✅ **Wave G.1** — `GetSoc2ComplianceReport` — SOC 2 Type II: 5 controlos CC6/CC7/CC8/CC9/A1 com scoring por releases e Evidence Packs assinados. CG: 662 testes.
12. ✅ **Wave G.2** — `GetIso27001ComplianceReport` — ISO 27001:2022: 5 controlos Annex A com scoring contextual. CG: 662 testes.
13. ✅ **Wave G.3** — `GraphQlSchemaSnapshot` + `AnalyzeGraphQlSchema` + `DetectGraphQlBreakingChanges` + `GetGraphQlSchemaHistory` — GraphQL SDL parsing leve + breaking change detection. Catalog: 1726 testes. **WAVE G COMPLETO**.
14. ✅ **Wave H.1** — `ProtobufSchemaSnapshot` + `AnalyzeProtobufSchema` + `DetectProtobufBreakingChanges` + `GetProtobufSchemaHistory` — Protobuf .proto parsing leve + breaking change detection (messages, fields, services, RPCs). Catalog: 1754 testes (+28).
15. ✅ **Wave H.2** — `GetPciDssComplianceReport` — PCI-DSS v4.0: 5 requisitos (Req 1-2, 6, 10, 11, 12) com scoring por releases e Evidence Packs assinados. CG: 673 testes (+11).
16. ✅ **Wave H.3** — `GetServiceMaturityScoreV2` — scorecard dimensional v2 com 6 dimensões, pesos por tier (Critical/Standard/Experimental) e postura de vulnerabilidade. Catalog: 1754 testes. **WAVE H COMPLETO**.
17. ✅ **Wave I.1** — `GetHipaaComplianceReport` — HIPAA Security Rule: 5 controlos (§164.312 a/b/c/d/e) com scoring contextual via releases e Evidence Packs assinados. CG: 685 testes (+12).
18. ✅ **Wave I.2** — `ServiceCostAllocationRecord` + `IngestServiceCostRecord` + `GetServiceCostAllocationReport` + `GetFinOpsInsights` — FinOps Contextual por serviço/equipa/ambiente com deteção de anomalias (outliers P75, não-prod > prod, crescimento de categoria). OI: 1004 testes (+12).
19. ✅ **Wave I.3** — `GetDependencyRiskReport` — risk scoring do grafo de dependências por tier + fan-in de APIs + governance gaps. Catalog: 1764 testes (+10). **WAVE I COMPLETO**.
20. ✅ **Wave J.1** — `GetGdprComplianceReport` — GDPR: 5 controlos (Art. 5/13/17/25/33) com scoring contextual via releases, evidence packs assinados e análise de integridade. CG: 709 testes (+11).
21. ✅ **Wave J.2** — `SloObservation` + `IngestSloObservation` + `GetSloComplianceSummary` + `GetSloViolationTrend` — SLO Tracking com classificação automática (Met/Warning/Breached), compliance summary e trend analysis (30 janelas diárias). OI: 1020 testes (+16).
22. ✅ **Wave J.3** — `GetChangeRollbackRecommendation` — scoring composto de urgência de rollback (0–100) por confidence + blast radius + evidence integrity. 4 níveis: None/Suggest/Recommend/Critical. CG: 709 testes (+24 total). **WAVE J COMPLETO**.
23. ✅ **Wave K.1** — `GetChaosExperimentReport` — analytics de chaos experiments: taxa de sucesso, distribuição por tipo/risco/estado, top 5 serviços, duração média. OI: 1033 testes (+13).
24. ✅ **Wave K.2** — `GetCmmcComplianceReport` — CMMC 2.0 Level 2: 5 práticas (AC.1.001, IA.1.076, AU.2.041, IR.2.092, RM.2.141) com scoring contextual via releases e Evidence Packs assinados. CG: 734 testes (+11).
25. ✅ **Wave K.3** — `GetChangeFrequencyHeatmap` + `GetDeploymentCadenceReport` — heatmap 7×24 de deployments + classificação DORA (HighPerformer/Medium/LowPerformer/Insufficient) por serviço. CG: 734 testes (+25 total). **WAVE K COMPLETO**.
26. ✅ **Wave L.1** — `GetServiceOwnershipHealthReport` — scorecard de saúde de ownership por serviço: 5 tipos de problema, 4 bandas de saúde, score 0–100 com pesos por gravidade. Catalog: 1783 testes (+19).
27. ✅ **Wave L.2** — `GetFedRampComplianceReport` — FedRAMP Moderate: 5 controlos NIST SP 800-53 (AC-2, AU-2, CM-6, IR-4, SI-2) com scoring contextual via releases e Evidence Packs. CG: 745 testes (+11).
28. ✅ **Wave L.3** — `GetOperationalReadinessReport` — scorecard pré-produção: 5 dimensões ponderadas (SLO 35%, Chaos 25%, Drift 20%, Profiling 10%, Baseline 10%), 3 classificações (Ready/Conditional/NotReady) com bloqueadores automáticos. OI: 1047 testes (+14). **WAVE L COMPLETO**.
29. ✅ **Wave M.1** — `GetContractHealthDistributionReport` — distribuição de scores de saúde de contratos: 4 bandas (Healthy/Fair/AtRisk/Critical), médias por dimensão (6 dimensões), top contratos críticos. Catalog: 1802 testes (+19).
30. ✅ **Wave M.2** — `GetTeamChangeVelocityReport` — velocidade de mudança por equipa: releases/semana, taxa de sucesso/falha/rollback, participação no tenant, `VelocityTier` (HighVolume/Moderate/LowFrequency/Inactive). CG: 760 testes (+15).
31. ✅ **Wave M.3** — `GetOpenDriftImpactSummary` — sumário de drifts abertos: serviços mais afetados, métricas mais desviantes, distribuição de severidade (Low/Medium/High/Critical), desvio médio e máximo global. OI: 1068 testes (+21). **WAVE M COMPLETO**.
32. ✅ **Wave N.1** — `GetSloServiceRankingReport` — ranking de serviços por taxa de conformidade SLO: bandas Excellent/Good/Struggling, médias de valor observado vs alvo, contagens de Met/Warning/Breached, tenant avg compliance rate. OI: 1081 testes (+13).
33. ✅ **Wave N.2** — `GetRiskTrendReport` — distribuição de risco por nível (Negligible/Low/Medium/High/Critical), top serviços de alto risco com score por dimensão (vuln 40%/change_failure 25%/blast_radius 20%/policy 15%), % alto/crítico no tenant. CG: 783 testes (+11).
34. ✅ **Wave N.3** — `GetEvidencePackCoverageReport` — cobertura de evidence packs por releases: coverage%, signed packs%, complete packs%, breakdown por ambiente, lista de releases sem cobertura (até N). CG: 783 testes (+12). **WAVE N COMPLETO**.
35. ✅ **Wave O.1** — `GetContractVersioningReport` — relatório de versionamento de contratos: distribuição por estado de ciclo de vida (Draft/InReview/Approved/Locked/Deprecated/Sunset/Retired), distribuição por protocolo (OpenAPI/AsyncAPI/GraphQL/Protobuf/WSDL), rácios obsoleto e ativo, top contratos deprecados/sunset candidatos a remoção. Catalog: 1813 testes (+11).
36. ✅ **Wave O.2** — `GetFinOpsTrendReport` — tendência de custo operacional: série temporal diária (DailySeries), distribuição por categoria (Compute/Storage/Network/…), top serviços mais dispendiosos, delta período-a-período (CurrentPeriodTotalUsd / PreviousPeriodTotalUsd / DeltaPercent). OI: 1098 testes (+17).
37. ✅ **Wave O.3** — `GetPromotionGateComplianceReport` — conformidade de promotion gates: total de avaliações pass/fail/overridden, taxa global de aprovação, distribuição por tipo de gate (SecurityGate/QualityGate/ApprovalGate/…), top gates que mais falham com fail rate. CG: 796 testes (+13). **WAVE O COMPLETO**.
38. ✅ **Wave P.1** — `GetServiceApiExposureReport` — mapa de exposição de APIs do tenant: total de serviços e APIs, serviços órfãos (sem APIs), serviços de alta exposição (API count ≥ threshold), distribuição por visibilidade (Public/Internal/Partner/Other), distribuição por ExposureType (Internal/External/Partner), top serviços por contagem de APIs, média de APIs por serviço. Catalog: 1831 testes (+18). Config: `catalog.api_exposure.*` sort 10880–10900.
39. ✅ **Wave P.2** — `GetResilienceScoreSummaryReport` — sumário de scores de resiliência pós-chaos: score médio global, classificação de tier (Poor/Fair/Good/Excellent), distribuição de relatórios por tier, top serviços mais resilientes e mais vulneráveis por score médio, tempo médio de recuperação, desvio médio de blast radius, distribuição por tipo de experimento. OI: 1120 testes (+22). Config: `resilience.score.*` sort 10910–10930.
40. ✅ **Wave P.3** — `GetReleaseSuccessRateReport` — taxa de sucesso de releases por serviço e ambiente: taxa global de sucesso/falha/rollback, distribuição por DeploymentStatus (Pending/Running/Succeeded/Failed/RolledBack), distribuição por ambiente, top serviços com maior taxa de falha, SuccessRateTier (Elite ≥99%/High ≥95%/Medium ≥80%/Low). CG: 816 testes (+20). Config: `release.success_rate.*` sort 10940–10950. **WAVE P COMPLETO**.

41. ✅ **Wave Q.1** — `GetRuntimeBaselineComparisonReport` — comparação de snapshots de runtime recentes contra baselines estabelecidas por serviço e ambiente. Para cada par (serviço, ambiente) com snapshot recente, calcula desvio percentual de latência média, latência P99, taxa de erro e throughput. Classifica drift em DriftSeverity (None / Minor / Moderate / Severe) usando o desvio máximo entre métricas. Produz: totais (monitorados / com baseline / sem baseline / com drift / drift severo), distribuição por severidade, top serviços com maior desvio composto, desvios médios de latência e erro a nível de tenant. OI: 1138 testes (+18). Config: `runtime.baseline.comparison.*` sort 10960–10980.

42. ✅ **Wave Q.2** — `GetContractConsumerImpactReport` — relatório de impacto nos consumidores de contratos em risco de remoção. Identifica contratos com LifecycleState Deprecated ou Sunset (opcionalmente Retired), e para cada um lista os consumidores activos com ConsumerExpectation registada. Produz: total de contratos em risco, total de expectativas de consumidores afetadas, serviços e domínios distintos, distribuição por estado de lifecycle, top contratos por número de consumidores, top domínios por exposição. Catalog: 1845 testes (+14). Config: `contracts.consumer_impact.*` sort 10990–11010.

43. ✅ **Wave Q.3** — `GetBlastRadiusDistributionReport` — distribuição do blast radius das releases no período. Agrega relatórios de blast radius calculados para todas as releases num intervalo temporal. Produz: totais de releases com/sem blast radius, médias de consumidores diretos e totais, distribuição por bucket de impacto (Zero=0 / Small=1–5 / Medium=6–20 / Large>20), top releases por total de consumidores afetados, top serviços por blast radius médio e máximo. CG: 833 testes (+17). Config: `changes.blast_radius.distribution.*` sort 11020–11030. **WAVE Q COMPLETO**.

44. ✅ **Wave R.1** — `GetIncidentChangeCorrelationReport` — correlação entre releases e incidentes pós-deploy por serviço. Analisa todas as releases no período e, para cada uma, verifica eventos `incident_correlated` na sua timeline. Agrega por serviço: total de releases, releases com incidente, taxa de incidente pós-deploy (%). Classifica risco em `IncidentCorrelationRisk` (Low <5% / Medium 5–15% / High 15–30% / Critical >30%). Produz: totais do tenant, distribuição por tier de risco, top serviços por taxa de incidente e por contagem absoluta. CG: 852 testes (+19). Config: `changes.incident_correlation.*` sort 11040–11050.

45. ✅ **Wave R.2** — `GetApiSchemaStabilityReport` — estabilidade de schemas de API por frequência de changelogs. Agrega entradas de `ContractChangelog` no período por `ApiAssetId` via `ListByTenantInPeriodAsync`. Classifica estabilidade em `SchemaStabilityTier` (Stable=0 / Volatile≥1 / Unstable≥3 / Critical≥6). Produz: total de contratos com alterações, avg e max de changelogs por contrato, distribuição por tier, top contratos mais instáveis e mais estáveis. Adicionado `ListByTenantInPeriodAsync` a `IContractChangelogRepository` e implementação correspondente no repositório de infraestrutura. Catalog: 1861 testes (+16). Config: `contracts.schema_stability.*` sort 11060–11080.

46. ✅ **Wave R.3** — `GetTeamOperationalHealthReport` — scorecard composto de saúde operacional por equipa. Usa nova abstração `ITeamOperationalMetricsReader` para obter métricas pré-agregadas por equipa (ServiceCount, SloComplianceRatePct, UnacknowledgedDriftCount, ChaosSuccessRatePct, ServicesWithProfilingCount, PostDeployIncidentCount). Computa score ponderado: SLO 40% + Drift 30% + Chaos 20% + Profiling 10%. Classifica em `OperationalHealthTier` (Excellent ≥90 / Good ≥70 / Fair ≥50 / Poor <50). Produz: média do tenant, distribuição por tier, top equipas saudáveis e em risco, ranking completo. OI: 1161 testes (+23). Config: `runtime.team_health.*` sort 11090–11110. **WAVE R COMPLETO**.
47. ✅ **Wave S.1** — `GetChangeWindowUtilizationReport` — conformidade de janelas de mudança: taxa de deploys dentro vs. fora de janela (Scheduled/HotfixAllowed), top equipas não-conformes, classificação Excellent/Good/AtRisk. Aprofunda Release Calendar (Wave F) como mecanismo de governance de deployment. CG. Config: `changes.window_utilization.*` sort 11120–11140.
48. ✅ **Wave S.2** — `GetContractAdoptionReport` — progresso de migração de versões de contrato pelos consumidores: taxa de adoção da versão mais recente por contrato, `MigrationTier` (Complete/InProgress/Lagging/NoConsumers), versão mais antiga ainda em uso, top contratos com migração mais lenta. Catalog. Config: `contracts.adoption.*` sort 11150–11170.
49. ✅ **Wave S.3** — `GetMttrTrendReport` — tendência de MTTR por serviço: classificação DORA (Elite/High/Medium/Low), série temporal diária (30 pontos), tendência Worsening/Improving/Stable/Insufficient, top serviços com pior MTTR e maior pioria. OI. Config: `runtime.mttr.*` sort 11180–11190. **Wave S COMPLETA.**
50. ✅ **Wave T.1** — `GetPostIncidentLearningReport` — taxa de aprendizado pós-incidente: % de incidentes com runbook aprovado pós-evento, incidentes recorrentes sem documentação (`LearningCoverage`: Full/Partial/Low), top serviços com menor learning rate. CG. Config: `compliance.learning.*` sort 11200–11220.
51. ✅ **Wave T.2** — `GetApiSchemaCoverageReport` — completude de documentação de schemas de API: score por 4 dimensões (response body, request body, exemplos, status codes), `CoverageGrade` A/B/C/D por contrato, distribuição global, top contratos com menor cobertura. Catalog. Config: `contracts.schema_coverage.*` sort 11230–11250.
52. ✅ **Wave T.3** — `GetEnvironmentStabilityReport` — score de estabilidade comparado por ambiente (dev/staging/prod): 4 dimensões ponderadas (SLO/Drift/Chaos/incident correlation), `StabilityTier` Stable/Unstable/Critical, flag de alerta "non-prod mais instável que prod", top serviços desestabilizadores. OI. Config: `runtime.environment_stability.*` sort 11260–11270. **Wave T COMPLETA**.
53. 🔲 **Wave U.1** — `GetComplianceCoverageMatrixReport` — matriz de cobertura de standards por serviço: quantos standards (SOC2/ISO27001/PCI-DSS/HIPAA/GDPR/FedRAMP/NIS2/CMMC) foram avaliados, `CoverageLevel` Full/Partial/Minimal/None, top serviços com maior gap de compliance, score de compliance por standard. CG. Config: `compliance.coverage.*` sort 11280–11300.
54. 🔲 **Wave U.2** — `GetDependencyUpdateFreshnessReport` — análise de frescor de dependências entre serviços: `FreshnessTier` Fresh/Aging/Stale/Critical por serviço, flag `VulnerabilityGap` para serviços Stale/Critical com vulns abertas, top serviços mais desatualizados com contagem de vulns. Catalog. Config: `catalog.dependency_freshness.*` sort 11310–11330.
55. 🔲 **Wave U.3** — `GetServiceLoadDistributionReport` — distribuição de carga operacional por serviço: `LoadBand` High/Medium/Low por quartil de throughput, correlação com custo (`CostPerRequestUsd`), flag `WasteCandidate` (baixo uso + alto custo), top 10 serviços com pior custo por request. OI. Config: `runtime.load_distribution.*` sort 11340–11350. **Wave U PLANEADA**.
56. 🔲 **Wave V.1** — `GetServiceApiGrowthReport` — taxa de crescimento de APIs por serviço: `GrowthTier` Stable/Growing/RapidGrowth/Exploding/Shrinking, `GovernanceRisk` flag (crescimento acelerado + qualidade baixa), top serviços com maior crescimento. Catalog. Config: `catalog.api_growth.*` sort 11360–11380.
57. 🔲 **Wave V.2** — `GetChaosCoverageGapReport` — gaps de cobertura de chaos engineering: `GapLevel` NoCoverage/ProductionGap/FailedCoverage/PartialCoverage/FullCoverage, flag `CriticalGap` para serviços Critical sem cobertura, `CoverageRate` global, top serviços críticos não cobertos. OI. Config: `chaos.coverage.*` sort 11390–11400.
58. 🔲 **Wave V.3** — `GetReleaseFrequencyDeviationReport` — desvio de frequência de deployment: compara período recente vs. histórico, `FrequencyDeviation` Accelerating/Stable/Decelerating/Stalled/New, `RiskFlag` para aceleração com baixo success rate ou serviços críticos parados. CG. Config: `changes.frequency_deviation.*` sort 11410–11430. **Wave V PLANEADA**.
59. 🔲 **Wave W.1** — `GetRollbackPatternReport` — padrões de rollback por serviço: `RollbackPattern` Isolated/Recurring/Serial/None, `SystemicRisk` flag (Serial + baixa confidence), `EvidenceGap` flag (rollbacks com evidence packs incompletos), correlação com ChangeConfidenceBreakdown. CG. Config: `changes.rollback_pattern.*` sort 11440–11460.
60. 🔲 **Wave W.2** — `GetServiceCouplingIndexReport` — índice de acoplamento entre serviços: `CouplingIndex` 0–100 por fan-in/fan-out, `CouplingTier` HubService/HighlyCoupled/ModeratelyCoupled/LooselyCoupled/Isolated, `ArchitecturalRisk` e `IsolationRisk` flags, % de serviços Isolated, CouplingIndex médio do tenant. Catalog. Config: `catalog.coupling_index.*` sort 11470–11490.
61. 🔲 **Wave W.3** — `GetAnomalyDetectionSummaryReport` — sumário consolidado de anomalias: agrega WasteSignal + DriftFinding + SLO breaches + Chaos failures + VulnerabilityAdvisory + incidentes pós-deploy por serviço, `AnomalyDensity` Clean/Moderate/Dense/Critical, lista de serviços multi-anomaly, timeline diária de 30 pontos. OI. Config: `runtime.anomaly_summary.*` sort 11500–11510. **Wave W PLANEADA**.
62. 🔲 **Wave X.1** — Executive Intelligence Dashboard — `ServiceHealthSummaryCard` + `ChangeConfidenceGauge` + `ComplianceCoverageWidget` + `FinOpsBudgetBurnWidget` + `TopRiskyServicesTable` + `MttrTrendMiniChart`. Persona Executive/CTO. Frontend. Config: `ui.executive_dashboard.*` sort 11520–11580.
63. 🔲 **Wave X.2** — GraphQL & Protobuf Visual Studio — `GraphQlSchemaDiffViewer` + `GraphQlSchemaExplorer` + `ProtobufSchemaDiffViewer` + `ProtobufSchemaExplorer` no Contract Studio. Frontend. Config: `ui.contract_studio.*` sort 11540–11550.
64. 🔲 **Wave X.3** — Persona-Aware Adaptive Navigation — reordenação de menu e quick actions por persona (`Engineer/TechLead/Architect/PlatformAdmin/Executive/Auditor`). Endpoint `GET /api/v1/identity/me/persona-config`. Frontend + Backend. Config: `ui.adaptive_navigation.*` sort 11560–11570. **Wave X PLANEADA**.
65. 🔲 **Wave Y.1** — Agentic Runtime com Human-in-the-Loop — `AgentExecutionPlan` aggregate + `AgentStep` multi-type + `ApproveAgentStep` gate + budget enforcement + audit trail por passo. AIKnowledge/AIOrchestration. Config: `ai.agentic.*` sort 11600–11620.
66. 🔲 **Wave Y.2** — NLP-based Model Routing — intent classifier leve (embedding) → `PromptIntent` enum → `ModelRoutingPolicy` entity + cost-aware downgrade automático + `GetModelRoutingDecisionLog`. AIKnowledge. Config: `ai.routing.*` sort 11630–11640.
67. 🔲 **Wave Y.3** — AI Token Budget Attribution — `AiTokenUsageRecord` + `GetAiTokenBudgetReport` + `GetAiCostAttributionReport` + `AiBudgetPolicy` enforcement por tenant/equipa. AIKnowledge + FinOps. Config: `ai.budget.*` sort 11650–11670. **Wave Y PLANEADA**.
68. 🔲 **Wave Z.1** — Kafka / Message Queue Consumer Real — `EventConsumerWorker` (`BackgroundService`) + `IEventNormalizationStrategy` por source type + dead letter queue + monitoring endpoint. Worker. Config: `integrations.event_consumer.*` sort 11680–11700.
69. 🔲 **Wave Z.2** — SDK NexTrace — CLI `nexone` + NuGet `NexTrace.Sdk` + npm `nexone-sdk` + GitHub Action `nexone/change-confidence-gate@v1`. Developer tooling. Config: `sdk.platform_api.*` sort 11710–11720.
70. 🔲 **Wave Z.3** — ClickHouse Analytics Provider — `IClickHouseAnalyticsWriter/Reader` adapter + schema MergeTree/SummingMergeTree + health check + `analytics.Provider` config switch. Infrastructure. Config: `analytics.clickhouse.*` sort 11730–11750. **Wave Z PLANEADA**.

71. 📘 **Wave AA — V3 Frontend Evolution** — 12 sub-waves (V3.1→V3.12) cobrindo: Dashboard Intelligence Foundation (variables, revisions, SharingPolicy granular), Query-driven Widgets + NQL, Live/Cross-filter/Drill-down, AI-assisted Dashboards + Notebooks, Frontend Platform Uplift (tokens v2, Command Palette, WCAG 2.2, Storybook, perf budgets), Governance/Reports/Embedding, Real-time Collaboration via CRDT (Yjs), Marketplace + Plugin SDK (Module Federation), Advanced NQL + Alerting from widget + PWA on-call, **Persona-first Experience Suites (7 homes: Engineer/Tech Lead/Architect/Product/Executive/Platform Admin/Auditor)**, **Source-of-Truth Consolidation Surfaces (11 Centers: Compliance/Risk/FinOps/Change Confidence/Release Calendar/Rollback/Blast Radius/Evidence Pack/Operational Readiness/Drift/SLO+Chaos+Learning)**, **Contract Studio Visual (REST/SOAP/AsyncAPI/GraphQL/Protobuf) + AI Agent Marketplace + IDE Extensions Console (VS + VS Code) + Break Glass/JIT + Licensing Admin + Knowledge Hub Bridge + DaC GitOps**. Plano detalhado em [`V3-EVOLUTION-FRONTEND-DASHBOARDS.md`](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md). Alinhada com Copilot Instructions §§6, 7, 11, 12, 13, 16, 17, 18. Estimativa agregada ≥525 testes dedicados, além da cobertura backend existente. **Wave AA PLANEADA**.

72. 🔲 **Wave AB.1** — `GetKnowledgeRelationGraph` — grafo semântico de relações entre entidades (Service/Contract/Runbook/Incident/Release/Team/OperationalNote): arestas nomeadas (OwnedBy/DependsOn/PublishesContract/ConsumesContract/CorrelatedWith/MitigatedBy), subgrafo por âncora (1–3 saltos), RelationStrength com decaimento temporal, KnowledgeGraphSummary. Catalog. Config: `catalog.knowledge_graph.*` sort 11760–11780.
73. 🔲 **Wave AB.2** — `GetContractLineageReport` — linhagem de versões de contrato: LineageNode por versão (autor, aprovador, datas de promoção/deprecation), BreakingChangeCount por transição, ConsumerImpactAtDeprecation, VersionRetentionDays, StabilityScore de linhagem, LineageSummary. Catalog. Config: `catalog.contract_lineage.*` sort 11790–11800.
74. 🔲 **Wave AB.3** — `GetIncidentKnowledgeBaseReport` — base de conhecimento de incidentes: ResolutionConfidence, MeanTimeToRunbook, RunbookEffectivenessScore por tipo de incidente, KnowledgeGap flag, StaleRunbook flag, KnowledgeMaturityScore global. OI. Config: `runtime.incident_knowledge.*` sort 11810–11830. **Wave AB PLANEADA**.
75. 🔲 **Wave AC.1** — `GetOnboardingHealthReport` — scorecard de completude de onboarding por serviço: 5 dimensões ponderadas (Ownership 20% + Contracts 25% + Runbook 20% + SLO 20% + Profiling 15%), OnboardingTier (Complete/Advanced/Basic/Minimal), TeamOnboardingAvg, TenantOnboardingScore ponderado por tier de serviço. Catalog. Config: `catalog.onboarding_health.*` sort 11840–11860.
76. 🔲 **Wave AC.2** — `GetDeveloperActivityReport` — atividade de developers na plataforma: TotalActions ponderado (contratos=3/runbooks=2/outros=1), ActivityTier (HighlyActive/Active/Occasional/Inactive) por percentil, TeamActivityScore, top 10 utilizadores/equipas, InactiveTeams list. IA/Audit. Config: `audit.developer_activity.*` sort 11870–11880.
77. 🔲 **Wave AC.3** — `GetPlatformAdoptionReport` — adoção de 7 capacidades core por equipa: SloTracking/ChaosEngineering/ContinuousProfiling/ComplianceReports/ChangeConfidence/ReleaseCalendar/AiAssistant, AdoptionTier (Pioneer/Adopter/Explorer/Laggard), CapabilityAdoptionRate global, GrowthOpportunity (capacidades <30% adoção). OI. Config: `platform.adoption.*` sort 11890–11910. **Wave AC PLANEADA**.
78. 🔲 **Wave AD.1** — `GetZeroTrustPostureReport` — postura Zero Trust por serviço: 4 dimensões (Authentication 30% + mTLS 25% + TokenRotation 20% + PolicyCoverage 25%), ZeroTrustTier (Enforced/Controlled/Partial/Exposed), CriticalExposure flag, TenantZeroTrustScore. CG. Config: `security.zero_trust.*` sort 11920–11930.
79. 🔲 **Wave AD.2** — `GetSecretsExposureRiskReport` — deteção de segredos em artefactos: pattern matching leve (API keys/JWT/connection strings/IPs/emails), ExposureRisk por artefacto (None/Low/Medium/High/Critical), AffectedArtifacts list, AuditTrail por deteção, sem dependências externas. Catalog. Config: `catalog.secrets_exposure.*` sort 11940–11950.
80. 🔲 **Wave AD.3** — `GetAccessPatternAnomalyReport` — anomalias de acesso: 5 AnomalySignal types (OffHours/VolumetricSpike/FirstAccessSensitive/UnusualResource/BulkExport), RiskScore composto por tipo+sensibilidade, AnomalyDensityByUser (risco insider threat), top recursos acedidos anomalamente. IA/Audit. Config: `audit.access_anomaly.*` sort 11960–11990. **Wave AD PLANEADA**.
81. 🔲 **Wave AE.1** — `ContractTestRecord` + `IngestContractTestResult` + `GetContractTestCoverageReport` — cobertura de testes de contrato: ingestão de resultados de Pact/contract testing via pipeline, CoverageTier (Full/Good/Partial/None), TestPassRate, UncoveredConsumerPairs, migration `ContractTestRecords`. Catalog. Config: `contracts.test_coverage.*` sort 12000–12020.
82. 🔲 **Wave AE.2** — `GetSchemaBreakingChangeImpactReport` — impacto transitivo de breaking changes: DirectConsumers + IndirectConsumers (até `max_hop_depth` saltos), ImpactScore ponderado por tier, BreakingChangeImpactTier (Contained/Moderate/Significant/Widespread), MitigationOptions, breakdown por ambiente. Catalog. Config: `contracts.breaking_change_impact.*` sort 12030–12040.
83. 🔲 **Wave AE.3** — `GetApiBackwardCompatibilityReport` — compatibilidade retroativa longitudinal: BreakingChangeRate, ConsumerAdoptionLag, BackwardCompatibilityScore, CompatibilityTier (Stable/Evolving/Volatile/Unstable), StagnationFlag, TenantCompatibilityIndex. Catalog. Config: `contracts.backward_compat.*` sort 12050–12070. **Wave AE PLANEADA**.

84. 🔲 **Wave AF.1** — `GetServiceLifecycleTransitionReport` — transições de estado no ciclo de vida dos serviços: DaysInCurrentState, StagnationFlag (Deprecated sem progresso), AcceleratedRetirementFlag (salto rápido para Sunset), BlockedTransitionFlag (consumidores Critical ativos). Catalog. Config: `catalog.lifecycle.*` sort 12080–12100.
85. 🔲 **Wave AF.2** — `GetServiceRetirementReadinessReport` — prontidão para retirada de serviço: RetirementReadinessScore (ConsumerMigrated 40% + ContractsDeprecated 25% + RunbookDocumented 15% + DependantsNotified 20%), RetirementReadinessTier (Ready/NearReady/Blocked/NotReady), BlockerList, MigrationProgress. Endpoint `/retirement-readiness`. Catalog. Config: `catalog.retirement_readiness.*` sort 12110–12120.
86. 🔲 **Wave AF.3** — `GetServiceMigrationProgressReport` — progresso de migração de consumidores de serviços deprecated: MigrationCompletionRate, MigrationTier (Complete/Advanced/InProgress/Lagging), EstimatedCompletionDate (linear projection), StuckConsumers, DailyMigrationTimeline 30d. Catalog. Config: `catalog.migration_progress.*` sort 12130–12150. **Wave AF PLANEADA**.
87. 🔲 **Wave AG.1** — `GetEnvironmentCostComparisonReport` — comparação de custo non-prod vs. prod por serviço: NonProdToProdRatio, EnvironmentEfficiencyTier (Optimal/Acceptable/Overprovisioned/WasteAlert), NonProdWasteCostUsd, TotalNonProdWasteUsd do tenant. OI. Config: `finops.environment_cost.*` sort 12160–12170.
88. 🔲 **Wave AG.2** — `GetCostPerReleaseReport` — custo operacional por release: PreRelease vs. PostRelease daily avg, CostDeltaPct, CostImpactTier (Neutral/CostSaving/MinorIncrease/MajorIncrease/CostSpike), WastedDeploymentCost flag (Failed+CostSpike). OI. Config: `finops.cost_per_release.*` sort 12180–12200.
89. 🔲 **Wave AG.3** — `GetFinOpsWasteAnalysisReport` — análise consolidada de waste: 4 categorias (IdleWaste/OverProvisioningWaste/FailedDeploymentWaste/DriftWaste), WasteScore 0–100, WasteTier (Clean/Minor/Significant/Critical), TotalEstimatedWasteUsd, WasteOpportunity (top 10 savings). OI. Config: `finops.waste_analysis.*` sort 12210–12230. **Wave AG PLANEADA**.
90. 🔲 **Wave AH.1** — `GetEventSchemaEvolutionReport` — evolução de schemas AsyncAPI/Kafka: BreakingSchemaChanges, EventSchemaStabilityTier (Stable/Evolving/Volatile/Unstable), MigrationLag flag (consumidores em versão antiga > `lag_alert_days`), top eventos mais instáveis. Catalog. Config: `catalog.event_schema.*` sort 12240–12250.
91. 🔲 **Wave AH.2** — `GetEventProducerConsumerBalanceReport` — equilíbrio produtor-consumidor de eventos: OrphanedEvents (produzidos sem consumidores), BlindConsumers (consumidores sem produtor registado), HighFanOutEvents (`FanOutRisk`), BalanceSummary global. Catalog. Config: `catalog.event_balance.*` sort 12260–12270.
92. 🔲 **Wave AH.3** — `GetEventContractComplianceReport` — conformidade de produtores com contratos AsyncAPI: SchemaComplianceRate, PayloadViolationCount, UnregisteredFields, MissingRequiredFields, ComplianceTier (Compliant/MinorViolations/Degraded/NonCompliant), ViolationTimeline 30d. Catalog. Config: `catalog.event_compliance.*` sort 12280–12310. **Wave AH PLANEADA**.
93. 🔲 **Wave AI.1** — `GetDeploymentRiskForecastReport` — previsão preditiva de risco de deployment: ForecastRiskScore (5 dimensões: HistoricalRollbackRate+EnvironmentInstability+ServiceRiskProfile+ChangeConfidenceInverse+RecentIncidentRate), RiskForecastTier (Low/Moderate/High/Critical), ForecastExplanation, RecommendedActions. Endpoint `/risk-forecast` por release. CG. Config: `changes.risk_forecast.*` sort 12320–12330.
94. 🔲 **Wave AI.2** — `GetCapacityTrendForecastReport` — projeção de capacidade por serviço: regressão linear sobre RuntimeSnapshot histórico, DaysToLatencyThreshold, DaysToErrorRateThreshold, ForecastAlertTier (Stable/WatchList/AtRisk/Imminent). OI. Config: `runtime.capacity_forecast.*` sort 12340–12360.
95. 🔲 **Wave AI.3** — `GetIncidentProbabilityReport` — probabilidade preditiva de incidente: IncidentProbabilityScore (5 sinais: OpenDrift+SloBreachTrend+ChaosGap+RecentHighRiskRelease+OpenVulns), IncidentProbabilityTier (Unlikely/Possible/Probable/Imminent), TenantRiskHeatmap, refresh via Quartz.NET job. OI. Config: `runtime.incident_probability.*` sort 12370–12390. **Wave AI PLANEADA**.

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
8. **Wave S (completa):** S.1 `GetChangeWindowUtilizationReport` + S.2 `GetContractAdoptionReport` + S.3 `GetMttrTrendReport`. CG: 866/Catalog: 1873/OI: 1178 testes. +8 config keys (sort 11120–11190). **Wave T (completa):** T.1 `GetPostIncidentLearningReport` + T.2 `GetApiSchemaCoverageReport` + T.3 `GetEnvironmentStabilityReport`. CG: 879/Catalog: 1885/OI: 1192 testes. +8 config keys (sort 11200–11270). **Waves U–W (planeadas):** 3 waves detalhadas na secção 15, cobrindo itens 53–61 da lista de priorização.
9. **Waves X–Z (planeadas):** 3 novas waves detalhadas na secção 15, cobrindo itens 62–70. Wave X: Frontend Intelligence (dashboards, visual builders, adaptive navigation). Wave Y: AI Governance Deep Dive (agentic runtime, NLP routing, token budget attribution). Wave Z: Integration Ecosystem Completion (Kafka consumer, SDK, ClickHouse). Adicionam 24 config keys (sort 11520–11750), 3×3–4 secções i18n.
10. **Status das secções 1–12 revisto (Abril 2026):** §1.1 GraphQL e §1.2 Protobuf marcados como ✅ implementados (Waves G.3/H.1). §8.4 ML Correlation marcado como ✅ implementado (Wave A.5). §9.1 Compliance Packs marcado como ✅ parcialmente implementado (8 standards via Waves G–L). Tabela Priorização Recomendada actualizada para reflectir estado real.
11. **Waves AB–AE (planeadas):** 4 novas waves adicionadas em Abril 2026 cobrindo itens 72–83 da lista de priorização. **Wave AB** (Knowledge Graph & Semantic Relations): AB.1 `GetKnowledgeRelationGraph` + AB.2 `GetContractLineageReport` + AB.3 `GetIncidentKnowledgeBaseReport`. +8 config keys (sort 11760–11830). **Wave AC** (Self-Service & Platform Adoption Intelligence): AC.1 `GetOnboardingHealthReport` + AC.2 `GetDeveloperActivityReport` + AC.3 `GetPlatformAdoptionReport`. +8 config keys (sort 11840–11910). **Wave AD** (Zero Trust & Security Posture Analytics): AD.1 `GetZeroTrustPostureReport` + AD.2 `GetSecretsExposureRiskReport` + AD.3 `GetAccessPatternAnomalyReport`. +8 config keys (sort 11920–11990). **Wave AE** (Contract Testing & API Backward Compatibility): AE.1 `ContractTestRecord`+`IngestContractTestResult`+`GetContractTestCoverageReport` + AE.2 `GetSchemaBreakingChangeImpactReport` + AE.3 `GetApiBackwardCompatibilityReport`. +8 config keys (sort 12000–12070). Total de config keys adicionadas: +32 (sort 11760–12070). Total de testes estimados: +186 (Catalog +~115, OI +~42, CG/IA +~29). Novas migrations: 1 (`ContractTestRecords` — Wave AE.1). i18n: +12 secções em 4 locales.
12. **Waves AF–AI (planeadas):** 4 novas waves adicionadas em Abril 2026 cobrindo itens 84–95 da lista de priorização. **Wave AF** (Service Lifecycle Governance): AF.1 `GetServiceLifecycleTransitionReport` + AF.2 `GetServiceRetirementReadinessReport` + AF.3 `GetServiceMigrationProgressReport`. +8 config keys (sort 12080–12150). **Wave AG** (FinOps Advanced Attribution): AG.1 `GetEnvironmentCostComparisonReport` + AG.2 `GetCostPerReleaseReport` + AG.3 `GetFinOpsWasteAnalysisReport`. +8 config keys (sort 12160–12230). **Wave AH** (Event-Driven Architecture Governance): AH.1 `GetEventSchemaEvolutionReport` + AH.2 `GetEventProducerConsumerBalanceReport` + AH.3 `GetEventContractComplianceReport`. +8 config keys (sort 12240–12310). **Wave AI** (Predictive Intelligence & Forecasting): AI.1 `GetDeploymentRiskForecastReport` + AI.2 `GetCapacityTrendForecastReport` + AI.3 `GetIncidentProbabilityReport`. +8 config keys (sort 12320–12390). 1 novo endpoint REST (`/risk-forecast`). 1 job Quartz.NET (refresh de incident probability). Total: +32 config keys (sort 12080–12390). Testes estimados: +210 (Catalog +~84, OI +~113, CG +~13). i18n: +12 secções em 4 locales.
