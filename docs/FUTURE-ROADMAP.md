# NexTraceOne — Future Roadmap

> **Data:** Abril 2026  
> **Estado atual:** ~98% implementado — todos os módulos core estão READY  
> **Waves concluídas:** A → R (46 features analytics/governance implementadas e testadas)  
> **Waves planeadas:** S → Z (24 features novas documentadas, aguardam implementação)  
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

### Wave T — Post-Incident Learning + API Schema Coverage + Environment Stability

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

**Totais estimados Wave T:** CG: ~879 testes (+12). Catalog: ~1887 testes (+14). OI: ~1195 testes (+16). Configuração: +8 config keys (sort 11200–11270). i18n: +3 secções (4 locales).

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
50. 🔲 **Wave T.1** — `GetPostIncidentLearningReport` — taxa de aprendizado pós-incidente: % de incidentes com runbook aprovado pós-evento, incidentes recorrentes sem documentação (`LearningCoverage`: Full/Partial/Low), top serviços com menor learning rate. CG. Config: `compliance.learning.*` sort 11200–11220.
51. 🔲 **Wave T.2** — `GetApiSchemaCoverageReport` — completude de documentação de schemas de API: score por 4 dimensões (response body, request body, exemplos, status codes), `CoverageGrade` A/B/C/D por contrato, distribuição global, top contratos com menor cobertura. Catalog. Config: `contracts.schema_coverage.*` sort 11230–11250.
52. 🔲 **Wave T.3** — `GetEnvironmentStabilityReport` — score de estabilidade comparado por ambiente (dev/staging/prod): 4 dimensões ponderadas (SLO/Drift/Chaos/incident correlation), `StabilityTier` Stable/Unstable/Critical, flag de alerta "non-prod mais instável que prod", top serviços desestabilizadores. OI. Config: `runtime.environment_stability.*` sort 11260–11270. **Wave T PLANEADA**.
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
8. **Wave S (completa):** S.1 `GetChangeWindowUtilizationReport` + S.2 `GetContractAdoptionReport` + S.3 `GetMttrTrendReport`. CG: 866/Catalog: 1873/OI: 1178 testes. +8 config keys (sort 11120–11190). **Wave T–W (planeadas):** 4 waves detalhadas na secção 15, cobrindo itens 50–61 da lista de priorização. Adicionam 32 config keys (sort 11200–11510), 4×3 secções i18n e estimativa de +120 testes distribuídos por CG/Catalog/OI.
9. **Waves X–Z (planeadas):** 3 novas waves detalhadas na secção 15, cobrindo itens 62–70. Wave X: Frontend Intelligence (dashboards, visual builders, adaptive navigation). Wave Y: AI Governance Deep Dive (agentic runtime, NLP routing, token budget attribution). Wave Z: Integration Ecosystem Completion (Kafka consumer, SDK, ClickHouse). Adicionam 24 config keys (sort 11520–11750), 3×3–4 secções i18n.
10. **Status das secções 1–12 revisto (Abril 2026):** §1.1 GraphQL e §1.2 Protobuf marcados como ✅ implementados (Waves G.3/H.1). §8.4 ML Correlation marcado como ✅ implementado (Wave A.5). §9.1 Compliance Packs marcado como ✅ parcialmente implementado (8 standards via Waves G–L). Tabela Priorização Recomendada actualizada para reflectir estado real.
