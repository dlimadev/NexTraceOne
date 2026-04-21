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
