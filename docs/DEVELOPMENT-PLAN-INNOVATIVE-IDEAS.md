# NexTraceOne — Plano de Desenvolvimento: 29 Ideias Inovadoras

> **Data:** Abril 2026  
> **Referência:** [BRAINSTORMING-INNOVATIVE-IDEAS.md](./BRAINSTORMING-INNOVATIVE-IDEAS.md)  
> **Arquitetura:** Modular Monolith — .NET 10 + React 18 + PostgreSQL 16  
> **Módulos existentes:** Catalog, ChangeGovernance, OperationalIntelligence, AIKnowledge, Governance, Configuration, IdentityAccess, AuditCompliance, Integrations, Knowledge, Notifications, ProductAnalytics  
> **Última actualização:** 2026-04-09 — Wave A+B completa: Ideias 1, 2 (Wave A), 3, 10, 16, 29 (Wave B) implementadas (backend + testes)

---

## Sumário

Este documento detalha o **plano de desenvolvimento** para cada uma das 29 ideias inovadoras aprovadas. Para cada ideia, especifica:

- Módulo(s) responsável(eis)
- Entidades de domínio necessárias
- Endpoints / Features backend
- Páginas / Componentes frontend
- Dependências de outras ideias
- Estimativa de complexidade
- Wave de implementação sugerido

---

## Organização em Waves

| Wave | Nome | Ideias | Foco |
|------|------|--------|------|
| **Wave A** | Contract & Change Core | 1, 2, 5, 9, 22 | Pilares fundamentais: contratos + mudanças |
| **Wave B** | AI-Powered Operations | 3, 10, 16, 29 | IA operacional com narrativas e feedback |
| **Wave C** | Operational Intelligence | 6, 8, 12, 25 | Knowledge graph, drift, previsão, maturidade |
| **Wave D** | Developer Experience | 14, 19, 24, 26 | Pipelines, IDE, onboarding, negociação |
| **Wave E** | Governance & Reliability | 7, 17, 20, 27 | Self-healing, schema advisor, playbooks, chaos |
| **Wave F** | Executive & FinOps | 13, 15, 18, 28 | Dashboards executivos, custo contextual |
| **Wave G** | Visualization & Marketplace | 4, 11, 21, 23 | 3D visualization, marketplace, simulador, licenças |

---

## Wave A — Contract & Change Core

> **Prioridade:** 🔴 Alta  
> **Justificativa:** Reforça os pilares centrais do NexTraceOne — Contract Governance e Change Intelligence

---

### ✅ Ideia 1 — Contract Health Score em Tempo Real

**Módulo:** Catalog (Contract subdomain)  
**Complexidade:** 🟠 Média-Alta  
**Estado:** ✅ Backend implementado — 44 testes passando

#### Entidades de Domínio

```
ContractHealthScore (entidade)
├── ContractId (FK)
├── OverallScore (0-100)
├── BreakingChangeFrequencyScore
├── ConsumerImpactScore
├── ReviewRecencyScore
├── ExampleCoverageScore
├── PolicyComplianceScore
├── DocumentationScore
├── CalculatedAt (DateTime)
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `ContractHealthScore` — AuditableEntity com 6 dimensões |
| **Command** | `RecalculateContractHealthScore` — disparo manual ou por evento |
| **Query** | `GetContractHealthScore`, `ListContractsWithHealthBelowThreshold` |
| **Background Job** | `ContractHealthScoreRecalculationJob` (Quartz) — recalcula periodicamente |
| **Evento** | `ContractHealthDegraded` — emitido quando score cai abaixo de threshold |
| **Policy** | Configuração de thresholds por organização |
| **Migration** | Nova tabela `cat_contract_health_scores` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Badge** | `ContractHealthBadge` — exibido no catálogo, dependências, dashboards |
| **Detail Panel** | `ContractHealthDetailPanel` — breakdown das 6 dimensões |
| **Page** | Integração na página de detalhe do contrato |
| **Alert** | Notificação quando score degrada |
| **Chart** | Gráfico temporal de evolução do score |

#### Dependências
- Nenhuma dependência de outras ideias
- Usa dados existentes: contratos, consumidores, breaking changes, políticas

#### i18n Keys
- `contract.health.score`, `contract.health.dimensions.*`, `contract.health.degraded`

---

### ✅ Ideia 2 — Change Confidence Timeline

**Módulo:** ChangeGovernance  
**Complexidade:** 🟠 Média  
**Estado:** ✅ Backend implementado — 34 testes passando

#### Entidades de Domínio

```
ChangeConfidenceEvent (entidade)
├── ChangeId (FK)
├── EventType (enum: Created, DevValidated, StagingTested, Approved, AnomalyDetected, Deployed)
├── ConfidenceBefore (0-100)
├── ConfidenceAfter (0-100)
├── Reason (string)
├── Details (JSONB)
├── OccurredAt (DateTime)
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `ChangeConfidenceEvent` — imutável, append-only |
| **Command** | `RecordConfidenceEvent` — chamado internamente por handlers de mudança |
| **Query** | `GetChangeConfidenceTimeline` — lista ordenada de eventos |
| **Integração** | Hooks nos handlers existentes: `CreateChange`, `ValidateChange`, `ApproveChange`, `DetectAnomaly`, `DeployChange` |
| **Migration** | Nova tabela `chg_change_confidence_events` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Timeline** | `ChangeConfidenceTimeline` — timeline interativa com drill-down |
| **Chart** | Gráfico de linha mostrando evolução do score |
| **Integration** | Widget na página de detalhe da mudança |

#### Dependências
- Nenhuma — usa infrastructure de changes existente

---

### Ideia 5 — Contract Diff Semântico com IA

**Módulo:** Catalog (Contract) + AIKnowledge  
**Complexidade:** 🔴 Alta

#### Entidades de Domínio

```
SemanticDiffResult (value object / JSONB)
├── ContractVersionFromId (FK)
├── ContractVersionToId (FK)
├── NaturalLanguageSummary (string)
├── Classification (Breaking | NonBreaking | Enhancement)
├── AffectedConsumers (List<ConsumerId>)
├── MitigationSuggestions (List<string>)
├── CompatibilityScore (0-100)
├── GeneratedByModel (string)
├── GeneratedAt (DateTime)
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateSemanticDiff` — command que invoca IA para análise |
| **AI Agent** | Extensão do `contract-designer` agent com capability `semantic-diff` |
| **Query** | `GetSemanticDiff` — retorna análise cached |
| **Service** | `SemanticDiffService` — orquestra diff textual + IA + consumer lookup |
| **Event** | `BreakingChangeDetected` — emitido com lista de consumidores afetados |
| **Migration** | Nova tabela `cat_semantic_diff_results` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ContractDiffPage` — side-by-side com painel IA |
| **Panel** | `SemanticDiffPanel` — resumo NLP + classificação + consumidores |
| **Badge** | `BreakingChangeBadge` no catálogo |

#### Dependências
- Ideia 1 (Contract Health Score) — breaking changes alimentam health score
- AI model com tool calling funcional (Qwen 2.5 32B recomendado)

---

### Ideia 9 — Smart Promotion Gates

**Módulo:** ChangeGovernance  
**Complexidade:** 🔴 Alta

#### Entidades de Domínio

```
PromotionGate (entidade)
├── Id
├── Name
├── EnvironmentFrom / EnvironmentTo
├── Rules (List<PromotionRule>) — JSONB
├── IsActive
├── CreatedBy
└── TenantId

PromotionGateEvaluation (entidade)
├── GateId (FK)
├── ChangeId (FK)
├── Result (Passed | Failed | Warning)
├── RuleResults (List<RuleResult>) — JSONB
├── EvaluatedAt
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `PromotionGate`, `PromotionGateEvaluation` |
| **Command** | `CreatePromotionGate`, `EvaluatePromotionGate` |
| **Query** | `GetPromotionGateStatus`, `ListGatesByEnvironment` |
| **Service** | `PromotionGateEngine` — avalia todas as regras combinadas |
| **Rules** | TestResult, ContractHealth, BlastRadius, IncidentHistory, ChangeWindow, AIConfidence, PendingApprovals |
| **Cross-module** | `ICatalogGraphModule` (blast radius), `IIncidentModule` (histórico) |
| **Migration** | Tabelas `chg_promotion_gates`, `chg_promotion_gate_evaluations` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `PromotionGateConfigPage` — CRUD de gates |
| **Panel** | `GateEvaluationPanel` — mostra resultado de cada regra com justificativa |
| **Widget** | Badge na página de promoção da mudança |

#### Dependências
- Ideia 1 (health score) e Ideia 2 (confidence) alimentam regras do gate

---

### Ideia 22 — Automated Contract Compliance Gate

**Módulo:** Catalog + ChangeGovernance  
**Complexidade:** 🟠 Média-Alta

#### Entidades de Domínio

```
ContractComplianceGate (entidade)
├── Id
├── Name
├── Rules (List<ContractComplianceRule>) — JSONB
├── Scope (Organization | Team | Environment)
├── ScopeId
├── BlockOnViolation (bool)
├── IsActive
└── TenantId

ContractComplianceResult (entidade)
├── GateId (FK)
├── ContractVersionId (FK)
├── ChangeId (FK, nullable)
├── Result (Pass | Warn | Block)
├── Violations (List<Violation>) — JSONB
├── EvidencePackId (FK, nullable)
├── EvaluatedAt
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `ContractComplianceGate`, `ContractComplianceResult` |
| **Command** | `CreateComplianceGate`, `EvaluateContractCompliance` |
| **Query** | `GetComplianceStatus`, `ListViolationsByContract` |
| **Rules** | BreakingChangeApproval, ExamplesRequired, HealthScoreMin, ConsumerCompatibility |
| **Event** | `ContractComplianceViolation` — gera evidence pack |
| **Migration** | Tabelas `cat_contract_compliance_gates`, `cat_contract_compliance_results` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ContractComplianceConfigPage` — gestão de gates |
| **Panel** | `ComplianceResultPanel` — resultado detalhado |
| **Badge** | Compliance badge no contrato e na mudança |

#### Dependências
- Ideia 1 (health score) — regra de health threshold
- Ideia 5 (semantic diff) — detecção de breaking changes

---

## Wave B — AI-Powered Operations

> **Prioridade:** 🟠 Média-Alta  
> **Justificativa:** IA operacional com narrativas, release notes e feedback loop

---

### ✅ Ideia 3 — AI-Powered Incident Narrator

**Módulo:** OperationalIntelligence + AIKnowledge  
**Complexidade:** 🟠 Média-Alta  
**Estado:** ✅ Backend implementado — 31 testes passando

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateIncidentNarrative` — command que invoca IA |
| **AI Agent** | Extensão do `incident-responder` com capability `narrative` |
| **Grounding** | Consulta: incident details + recent changes + affected services + runbooks |
| **Storage** | `IncidentNarrative` value object persistido no incidente (JSONB) |
| **Auto-trigger** | Handler reage a `IncidentCreated` event para gerar narrativa automaticamente |
| **Update** | `RefreshIncidentNarrative` — recalcula com novas informações |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `IncidentNarrativePanel` — texto formatado com links para entidades |
| **Actions** | Botão "Regenerar Narrativa", "Copiar para Post-mortem" |
| **Integration** | Widget na página de detalhe do incidente |

#### Dependências
- AI model funcional com context window grande (para grounding de múltiplas fontes)

---

### ✅ Ideia 10 — AI-Generated Release Notes

**Módulo:** ChangeGovernance + AIKnowledge  
**Complexidade:** 🟠 Média  
**Estado:** ✅ Backend implementado — 29 testes passando

#### Entidades de Domínio

```
ReleaseNotes (entidade)
├── ReleaseId (FK)
├── TechnicalSummary (string)
├── ExecutiveSummary (string?)
├── NewEndpointsSection (string?)
├── BreakingChangesSection (string?)
├── AffectedServicesSection (string?)
├── ConfidenceMetricsSection (string?)
├── EvidenceLinksSection (string?)
├── ModelUsed (string)
├── TokensUsed (int)
├── Status (ReleaseNotesStatus: Draft/Published/Archived)
├── TenantId (Guid?)
├── GeneratedAt
├── LastRegeneratedAt
└── RegenerationCount
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateReleaseNotes` — command que agrega mudanças + invoca IA |
| **Query** | `GetReleaseNotes` — retorna release notes por release/versão |
| **AI Agent** | Novo agent `release-notes-generator` ou extensão do `docs-assistant` |
| **Persona Modes** | Formato técnico (Engineer) vs executivo (Executive) |
| **Storage** | `ReleaseNotes` entidade com versões por persona |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ReleaseNotesPage` — lista de releases com notas geradas |
| **Toggle** | Switch técnico/executivo |
| **Export** | Markdown, PDF |
| **Integration** | Link na página de detalhe da release |

---

### ✅ Ideia 16 — Observability Anomaly Narratives

**Módulo:** OperationalIntelligence + AIKnowledge  
**Complexidade:** 🟠 Média  
**Estado:** ✅ Backend implementado — 31 testes passando

#### Entidades de Domínio

```
AnomalyNarrative (entidade)
├── DriftFindingId (FK)
├── NarrativeText (string)
├── SymptomsSection (string?)
├── BaselineComparisonSection (string?)
├── ProbableCauseSection (string?)
├── CorrelatedChangesSection (string?)
├── RecommendedActionsSection (string?)
├── SeverityJustificationSection (string?)
├── ModelUsed (string)
├── TokensUsed (int)
├── Status (AnomalyNarrativeStatus: Draft/Published/Stale)
├── TenantId (Guid?)
├── GeneratedAt
├── LastRefreshedAt
└── RefreshCount
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateAnomalyNarrative` — invocado quando anomalia é detectada |
| **AI Grounding** | Dados de telemetria + mudanças recentes + runbooks + baseline |
| **Storage** | `AnomalyNarrative` JSONB no `DriftFinding` ou entidade separada |
| **Severity** | IA classifica severidade com justificativa |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `AnomalyNarrativePanel` — explicação em linguagem natural |
| **Integration** | Widget no Drift Detection dashboard e no alert |
| **Actions** | "Criar incidente a partir desta anomalia" |

#### Dependências
- Ideia 3 (Incident Narrator) — padrão de narrativa reutilizado

---

### ✅ Ideia 29 — AI Knowledge Feedback Loop

**Módulo:** AIKnowledge  
**Complexidade:** 🟠 Média  
**Estado:** ✅ Backend implementado — 40 testes passando

#### Entidades de Domínio

```
AiFeedback (entidade)
├── Id
├── ConversationId (FK)
├── MessageId
├── Rating (Positive | Negative | Neutral)
├── Comment (string, nullable)
├── AgentName (string)
├── ModelUsed (string)
├── QueryCategory (string)
├── CreatedBy (UserId)
├── CreatedAt
└── TenantId

AiKnowledgeEntry (entidade — evolução)
├── SourceType (HumanValidated | AiGenerated | Feedback)
├── ValidationStatus (Pending | Approved | Rejected)
├── ValidatedBy (UserId, nullable)
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Command** | `SubmitAiFeedback` — regista avaliação do utilizador |
| **Command** | `PromoteFeedbackToKnowledge` — admin aprova resposta como conhecimento oficial |
| **Query** | `GetFeedbackMetrics` — satisfação por agente/modelo/tipo |
| **Query** | `ListNegativeFeedbackPatterns` — agrupa falhas para identificar gaps |
| **Background Job** | `FeedbackAnalysisJob` — analisa padrões periodicamente |
| **Alert** | `AiSatisfactionDegraded` — quando taxa de negativo ultrapassa threshold |
| **Migration** | Tabela `ai_feedbacks` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Inline** | Botões 👍/👎 em cada resposta da IA no chat |
| **Modal** | `FeedbackDetailModal` — comentário opcional |
| **Dashboard** | `AiFeedbackDashboard` — métricas de satisfação |
| **Admin Page** | `KnowledgePromotionPage` — aprovar respostas como conhecimento |

---

## Wave C — Operational Intelligence

> **Prioridade:** 🟠 Média-Alta  
> **Justificativa:** Eleva o NexTraceOne de reativo para preditivo e conectado

---

### Ideia 6 — Operational Knowledge Graph

**Módulo:** Knowledge + Cross-module  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `KnowledgeGraphNode`, `KnowledgeGraphEdge` — representação genérica |
| **Builder** | `KnowledgeGraphBuilder` — constrói graph a partir de todas as entidades |
| **Cross-module** | Consulta: Catalog (serviços, contratos, dependências), ChangeGovernance (mudanças, deploys), OI (incidentes), Knowledge (runbooks, docs) |
| **Query** | `GetGraphNeighborhood` — subgraph centrado numa entidade |
| **Query** | `SearchGraph` — pesquisa por entidade/relação |
| **AI Integration** | Graph como fonte de contexto para grounding |
| **Background Job** | `KnowledgeGraphRebuildJob` — reconstrói periodicamente |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `KnowledgeGraphExplorer` — visualização interativa |
| **Visualization** | Graph 2D navegável (D3.js ou similar) |
| **Search** | Pesquisa por entidade com navegação contextual |
| **Drill-down** | Click em nó → detalhe da entidade |

#### Dependências
- Nenhuma forte — agrega dados existentes de múltiplos módulos

---

### Ideia 8 — Environment Drift Detective

**Módulo:** OperationalIntelligence  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `DetectEnvironmentDrift` — compara ambientes |
| **AI Agent** | Novo agent `drift-detective` ou extensão do `service-analyst` |
| **Dimensions** | ServiceVersions, Configurations, ContractVersions, Dependencies, Policies |
| **Report** | `DriftReport` — severidade + recomendações |
| **Background Job** | `EnvironmentDriftDetectionJob` — execução periódica |
| **Storage** | Reutiliza `DriftFinding` existente com novos `DriftType` enums |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `EnvironmentDriftReport` — comparação visual entre ambientes |
| **Matrix** | Tabela comparativa env × serviço × versão |
| **Recommendations** | Painel de recomendações de correção |

#### Dependências
- Nenhuma forte — usa dados existentes de environments e catálogo

---

### Ideia 12 — Predictive Incident Prevention

**Módulo:** OperationalIntelligence + AIKnowledge  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `AnalyzePredictivePatterns` — analisa histórico para identificar padrões |
| **AI Integration** | ML/NLP para correlação temporal: deploy × dia × serviço × incidente |
| **Rules Engine** | Pattern matching: "3 de 5 deploys na sexta geraram incidente" |
| **Alert** | `PredictiveIncidentAlert` — alerta preventivo com contexto |
| **Storage** | `PredictivePattern` entidade com evidence histórica |
| **Background Job** | `PredictiveAnalysisJob` — execução diária |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `PredictiveAlertPanel` — alertas preventivos no dashboard |
| **Page** | `PredictivePatternsPage` — padrões identificados com evidência |
| **Widget** | Warning badge na criação de mudança quando padrão de risco é detectado |

#### Dependências
- Dados históricos de incidentes e mudanças (existentes)

---

### Ideia 25 — Service Maturity Model Tracker

**Módulo:** Governance  
**Complexidade:** 🟠 Média

#### Entidades de Domínio

```
ServiceMaturityAssessment (entidade)
├── ServiceId (FK)
├── CurrentLevel (1-5)
├── LevelDetails (JSONB) — checklist por nível
├── AssessedAt
├── AssessedBy (auto ou manual)
├── TenantId

ServiceMaturityCriteria (config)
├── Level (1-5)
├── CriteriaName
├── IsRequired (bool)
├── AutoEvaluable (bool) — pode ser verificado automaticamente
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Command** | `AssessServiceMaturity` — avalia nível atual |
| **Query** | `GetServiceMaturity`, `ListServicesByMaturityLevel` |
| **Auto-eval** | Verifica automaticamente: ownership? contratos? telemetria? runbooks? |
| **Cross-module** | Catalog (ownership, contratos), OI (telemetria, chaos), Knowledge (runbooks) |
| **Background Job** | `ServiceMaturityReassessmentJob` — reavalia periodicamente |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ServiceMaturityDashboard` — visão geral com filtros |
| **Detail** | `MaturityDetailPanel` — checklist por nível com ações |
| **Chart** | Distribuição por nível, evolução temporal |
| **Badge** | Maturity badge no catálogo de serviços |

---

## Wave D — Developer Experience

> **Prioridade:** 🟡 Média  
> **Justificativa:** Acelera developers e melhora colaboração entre equipas

---

### Ideia 14 — Contract-to-Code Pipeline Automatizado

**Módulo:** Catalog + AIKnowledge  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `ExecuteContractPipeline` — command que orquestra todas as etapas |
| **Stages** | ServerStubs, ClientSDK, MockServer, PostmanCollection, ContractTests, FitnessValidation |
| **AI Agents** | Reutiliza `contract-pipeline-agent`, `architecture-fitness-agent`, `test-generator` |
| **Storage** | `PipelineExecution` — histórico de execuções com artefactos |
| **Output** | Artefactos gerados como downloads (zip) ou push para repo |
| **Audit** | Cada execução auditada com input/output/model |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ContractPipelinePage` — wizard de configuração |
| **Step Flow** | Seleção de estágios, preview de output, confirmação |
| **Results** | Download de artefactos, logs de execução |
| **Integration** | Botão "Gerar Código" na página de detalhe do contrato |

#### Dependências
- AI agents existentes (`contract-pipeline-agent`, `architecture-fitness-agent`)
- Contrato OpenAPI/AsyncAPI/WSDL válido como input

---

### Ideia 19 — AI Pair Programming Governado

**Módulo:** AIKnowledge + Integrations (IDE Extensions)  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **API** | `POST /api/v1/ai/ide/query` — endpoint para extensões IDE |
| **Context** | Catálogo de serviços, contratos existentes, ownership, dependências |
| **Features** | ContractSuggestion, BreakingChangeAlert, OwnershipLookup, TestGeneration |
| **Governance** | Token budget per user, auditoria, política de modelo |
| **Auth** | API key ou token de sessão para IDE |

#### IDE Extensions

| Extensão | Stack |
|----------|-------|
| **VS Code** | TypeScript, VS Code Extension API |
| **Visual Studio** | C#, VSIX |

#### Dependências
- API pública estável com autenticação via token
- AI model com tool calling

---

### Ideia 24 — AI-Powered Onboarding Companion

**Módulo:** AIKnowledge  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **AI Agent** | Novo agent `onboarding-companion` |
| **System Prompt** | Contexto de: serviços da equipa, contratos, dependências, runbooks |
| **Grounding** | Catálogo filtrado por equipa do novo membro |
| **Personalization** | Nível de experiência (junior/mid/senior) |
| **Suggestions** | Issues/tasks adequados via integração com project management |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `OnboardingWizard` — fluxo guiado para novos membros |
| **Chat** | Chat dedicado com contexto de onboarding |
| **Progress** | Checklist de onboarding com items verificáveis |

#### Dependências
- Dados de equipa e ownership existentes no Catalog + IdentityAccess

---

### Ideia 26 — Cross-Team Contract Negotiation Workspace

**Módulo:** Catalog (Contract)  
**Complexidade:** 🔴 Alta

#### Entidades de Domínio

```
ContractNegotiation (entidade)
├── Id
├── ContractId (FK, nullable — pode ser novo contrato)
├── ProposedBy (TeamId)
├── Title
├── Description
├── Status (Draft | InReview | Negotiating | Approved | Rejected)
├── Deadline (DateTime, nullable)
├── Participants (List<TeamId>) — JSONB
├── TenantId

NegotiationComment (entidade)
├── NegotiationId (FK)
├── AuthorId (UserId)
├── Content
├── LineReference (nullable — inline comment)
├── CreatedAt
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidades** | `ContractNegotiation`, `NegotiationComment` |
| **Commands** | `CreateNegotiation`, `AddComment`, `ApproveNegotiation`, `RejectNegotiation` |
| **Queries** | `ListNegotiations`, `GetNegotiationDetail` |
| **Notifications** | Notificações para participantes em cada ação |
| **AI** | `contract-designer` agent sugere compromissos |
| **Migration** | Tabelas `cat_contract_negotiations`, `cat_negotiation_comments` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ContractNegotiationWorkspace` — split view: contrato + discussão |
| **Comments** | Inline comments similar a PR review |
| **Diff** | Diff semântico em tempo real |
| **Timeline** | Histórico de discussão e decisões |

---

## Wave E — Governance & Reliability

> **Prioridade:** 🟡 Média  
> **Justificativa:** Robustece operação com self-healing, schema governance e chaos engineering

---

### Ideia 7 — Self-Healing Recommendations

**Módulo:** OperationalIntelligence  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateHealingRecommendation` — invocado após root cause identificado |
| **AI Integration** | Consulta runbooks existentes + padrões históricos |
| **Actions** | Auto-remediation com aprovação (restart, scale, rollback, config change) |
| **Audit** | Toda ação auto-registada com evidence trail |
| **Learning** | Padrões de sucesso alimentam recomendações futuras |
| **Storage** | `HealingRecommendation`, `HealingExecution` entidades |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `HealingRecommendationPanel` — sugestões com botão "Executar" |
| **Approval** | Modal de aprovação para ações automatizáveis |
| **History** | Histórico de ações executadas |

---

### Ideia 17 — Schema Evolution Advisor

**Módulo:** Catalog (Contract) + AIKnowledge  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **AI Agent** | Novo agent `schema-evolution-advisor` |
| **Analysis** | Compatibilidade backward/forward, detecção de campos em uso |
| **Cross-module** | Consumer lookup via `ICatalogGraphModule` |
| **Suggestions** | Estratégias de migração: dual-write, versioning, transformation |
| **Output** | JSON estruturado com compatibilidade score e recomendações |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `SchemaEvolutionPanel` — integrado na edição de contrato |
| **Warnings** | Alertas inline quando campo removido ainda tem consumidores |
| **Suggestions** | Sugestões de migração com exemplos |

---

### Ideia 20 — Operational Playbook Builder

**Módulo:** OperationalIntelligence (ou Knowledge)  
**Complexidade:** 🟠 Média-Alta

#### Entidades de Domínio

```
OperationalPlaybook (entidade)
├── Id
├── Name
├── Description
├── Version
├── Steps (List<PlaybookStep>) — JSONB
├── Status (Draft | Active | Deprecated)
├── LinkedServiceIds (List<Guid>) — JSONB
├── LinkedRunbookIds (List<Guid>) — JSONB
├── ApprovedBy (UserId, nullable)
├── TenantId

PlaybookExecution (entidade)
├── PlaybookId (FK)
├── IncidentId (FK, nullable)
├── ExecutedBy (UserId)
├── StepResults (JSONB)
├── StartedAt / CompletedAt
├── Evidence (JSONB)
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidades** | `OperationalPlaybook`, `PlaybookExecution` |
| **Commands** | `CreatePlaybook`, `ExecutePlaybook`, `RecordStepResult` |
| **Queries** | `ListPlaybooks`, `GetPlaybookDetail`, `GetExecutionHistory` |
| **Versioning** | Playbooks versionados como contratos |
| **Approval** | Workflow de aprovação antes de ativação |
| **Migration** | Tabelas `ops_operational_playbooks`, `ops_playbook_executions` |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Builder** | `PlaybookBuilder` — editor visual drag-and-drop |
| **Steps** | Passos com decisões condicionais (if/else) |
| **Execution** | Formulário de execução passo-a-passo com evidências |
| **History** | Timeline de execuções anteriores |

---

### Ideia 27 — Chaos Engineering Integration Hub

**Módulo:** OperationalIntelligence  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Evolução** | Estende `ChaosExperiment` existente com integração real |
| **Features** | `DesignChaosExperiment` (baseado em topology), `MonitorExperimentImpact` |
| **Telemetry** | Monitora métricas durante experimento via telemetria existente |
| **Report** | `ResilienceReport` — comparação blast radius teórico vs real |
| **Integration** | Adapters para Chaos Monkey, Litmus, Gremlin |
| **Cross-module** | Alimenta Ideia 25 (Service Maturity nível 5) |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ChaosEngineeringHub` — design, execução e resultados |
| **Topology** | Seleção visual de serviço alvo no graph |
| **Monitor** | Dashboard real-time durante experimento |
| **Report** | Relatório de resiliência post-experiment |

#### Dependências
- `ChaosExperiment` entity existente (OI module)
- Telemetria funcional para monitoramento durante experimento
- Ideia 25 (Maturity Model) — optional cross-reference

---

## Wave F — Executive & FinOps

> **Prioridade:** 🟢 Exploratória  
> **Justificativa:** Funcionalidades de alto valor para personas executivas e gestão de custo

---

### Ideia 13 — Team Health Dashboard

**Módulo:** Governance  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Query** | `GetTeamHealthDashboard` — agrega métricas de múltiplos módulos |
| **Dimensions** | ServiceCount, ContractHealth, IncidentFrequency, MTTR, TechDebt, DocCoverage, PolicyCompliance |
| **Cross-module** | Catalog (serviços, contratos), OI (incidentes), Governance (debt, compliance) |
| **Cache** | Resultados cached com TTL configurável |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `TeamHealthDashboard` — cards por equipa |
| **Charts** | Radar chart das dimensões, evolução temporal |
| **Filters** | Por domínio, por período |
| **Drill-down** | Click na equipa → detalhe de cada dimensão |

---

### Ideia 15 — FinOps por Mudança

**Módulo:** Governance (FinOps subdomain) + ChangeGovernance  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `ChangeCostImpact` — custo incremental pós-deploy |
| **Integration** | Adapters para AWS Cost Explorer, Azure Cost Management, GCP Billing |
| **Correlation** | Match deploy timestamp → custo incremental do serviço |
| **Query** | `GetChangeCostImpact`, `ListCostliestChanges` |
| **Background Job** | `CostCorrelationJob` — correlaciona periodicamente |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Panel** | `ChangeCostPanel` — na página de detalhe da mudança |
| **Dashboard** | `FinOpsByChangeDashboard` — top mudanças por impacto de custo |
| **Chart** | Timeline custo × deploys |

---

### Ideia 18 — Executive Briefing Generator

**Módulo:** Governance + AIKnowledge  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `GenerateExecutiveBriefing` — agrega KPIs + invoca IA |
| **Sections** | PlataformStatus, TopIncidents, TeamPerformance, HighRiskChanges, ComplianceStatus, CostTrends, ActiveRisks |
| **Scheduling** | Geração periódica (diária/semanal/mensal) via Quartz |
| **AI** | IA gera sumário executivo a partir dos dados estruturados |
| **Storage** | `ExecutiveBriefing` entidade com conteúdo por secção |
| **Distribution** | Email + notificação in-app |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ExecutiveBriefingPage` — lista de briefings gerados |
| **Detail** | Visualização formatada com gráficos inline |
| **Export** | PDF, email digest |
| **Config** | Frequência e secções configuráveis |

---

### Ideia 28 — Operational Cost Attribution Engine

**Módulo:** Governance (FinOps)  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidade** | `CostAttribution` — custo por serviço/equipa/domínio/contrato/mudança |
| **Engine** | `CostAttributionEngine` — distribui custos com base em telemetria |
| **Data Sources** | Telemetria (consumo de recursos), Catalog (ownership), ChangeGovernance (mudanças) |
| **Queries** | `GetCostByDomain`, `GetCostByTeam`, `GetCostByService`, `GetCostByChange` |
| **Background Job** | `CostAttributionJob` — calcula periodicamente |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Dashboard** | `CostAttributionDashboard` — treemap de custos por dimensão |
| **Drill-down** | Domínio → Equipa → Serviço → Contrato |
| **Charts** | Evolução temporal, comparação entre períodos |
| **Alerts** | Anomalias de custo por serviço/equipa |

#### Dependências
- Ideia 15 (FinOps por Mudança) — reutiliza cost data sources

---

## Wave G — Visualization & Marketplace

> **Prioridade:** 🟢 Exploratória  
> **Justificativa:** Diferenciais visuais e marketplace interno de contratos

---

### Ideia 4 — Blast Radius Visualization 3D

**Módulo:** ChangeGovernance (frontend-heavy)  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Query** | `GetBlastRadiusGraph` — retorna graph de impacto com profundidade configurável |
| **Data** | Reutiliza topology existente do `ICatalogGraphModule` |
| **Enrich** | Criticidade, tipo de dependência, risco |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `BlastRadiusVisualization` — canvas 3D interativo |
| **Library** | Three.js ou react-three-fiber para 3D |
| **Fallback** | D3.js 2D para browsers sem WebGL |
| **Interactions** | Rotação, zoom, click-to-detail, filtros |
| **Legend** | Cor = risco, tamanho = criticidade, linha = tipo dependência |

---

### Ideia 11 — Contract Marketplace Interno

**Módulo:** Catalog  
**Complexidade:** 🟠 Média

#### Entidades de Domínio

```
ContractListing (entidade)
├── ContractId (FK)
├── Category (string)
├── Tags (List<string>) — JSONB
├── ConsumerCount (int, cached)
├── Rating (decimal, 0-5)
├── IsPromoted (bool)
└── TenantId

ContractReview (entidade)
├── ContractId (FK)
├── AuthorId (UserId)
├── Rating (1-5)
├── Comment (string)
├── CreatedAt
└── TenantId
```

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Entidades** | `ContractListing`, `ContractReview` |
| **Commands** | `PublishToMarketplace`, `SubmitReview`, `SuggestImprovement` |
| **Queries** | `SearchMarketplace`, `GetTopContracts`, `GetContractReviews` |
| **Search** | PostgreSQL FTS no marketplace |
| **Metrics** | Consumer count, adoption rate, review score |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ContractMarketplacePage` — grid com pesquisa e filtros |
| **Card** | `ContractCard` — preview com rating, consumers, tags |
| **Detail** | `MarketplaceDetailPanel` — reviews, exemplos, adoption |
| **Actions** | "Publicar", "Importar", "Avaliar" |

---

### Ideia 21 — Service Dependency Impact Simulator

**Módulo:** Catalog + ChangeGovernance  
**Complexidade:** 🔴 Alta

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `SimulateDependencyImpact` — command com cenário "what-if" |
| **Scenarios** | EndpointRemoval, ServiceUnavailability, ContractMigration, SchemaChange |
| **Engine** | `ImpactSimulationEngine` — percorre topology graph com cascata |
| **Output** | `SimulationResult` — serviços afetados, consumidores quebrados, risco % |
| **AI** | IA gera recomendações de mitigação para cenário |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Page** | `ImpactSimulatorPage` — formulário de cenário + visualização |
| **Form** | Seleção de serviço + tipo de cenário |
| **Results** | Graph de impacto + tabela de afetados + recomendações |
| **Compare** | Comparar múltiplos cenários side-by-side |

---

### Ideia 23 — Dependency License Compliance Radar

**Módulo:** Governance  
**Complexidade:** 🟠 Média

#### Backend

| Componente | Descrição |
|-----------|-----------|
| **Feature** | `AnalyzeLicenseCompliance` — scans dependências de serviços |
| **Integration** | Reutiliza dados do `dependency-advisor` agent e SBOM |
| **Rules** | Incompatível com comercial, conflito de licenças, mudança de licença |
| **Report** | `LicenseComplianceReport` — por serviço/equipa/domínio |
| **Alert** | Notificação quando licença muda em update |
| **Background Job** | `LicenseComplianceScanJob` — execução semanal |

#### Frontend

| Componente | Descrição |
|-----------|-----------|
| **Dashboard** | `LicenseComplianceRadar` — visualização radar por domínio |
| **Detail** | Lista de dependências com status de licença |
| **Report** | Export de relatório para auditoria |

---

## Resumo de Impacto por Módulo

| Módulo | Ideias | Novas Entidades |
|--------|--------|----------------|
| **Catalog** | 1, 5, 11, 21, 22, 26 | ContractHealthScore, SemanticDiffResult, ContractListing, ContractReview, ContractNegotiation, ContractComplianceGate |
| **ChangeGovernance** | 2, 4, 9 | ChangeConfidenceEvent, PromotionGate, PromotionGateEvaluation |
| **OperationalIntelligence** | 3, 7, 8, 16, 20, 27 | IncidentNarrative, HealingRecommendation, OperationalPlaybook, PlaybookExecution |
| **AIKnowledge** | 5, 10, 17, 19, 24, 29 | AiFeedback, ReleaseNotes |
| **Governance** | 13, 15, 18, 23, 25, 28 | ServiceMaturityAssessment, CostAttribution, ExecutiveBriefing, LicenseComplianceReport, ChangeCostImpact |
| **Knowledge** | 6 | KnowledgeGraphNode, KnowledgeGraphEdge |

---

## Estimativa de Esforço Global

| Wave | Ideias | Complexidade | Estimativa |
|------|--------|-------------|-----------|
| **A** — Contract & Change Core | 1, 2, 5, 9, 22 | 🔴 Alta | 6–8 semanas |
| **B** — AI-Powered Operations | 3, 10, 16, 29 | 🟠 Média-Alta | 4–6 semanas |
| **C** — Operational Intelligence | 6, 8, 12, 25 | 🟠 Média-Alta | 5–7 semanas |
| **D** — Developer Experience | 14, 19, 24, 26 | 🔴 Alta | 8–10 semanas |
| **E** — Governance & Reliability | 7, 17, 20, 27 | 🔴 Alta | 6–8 semanas |
| **F** — Executive & FinOps | 13, 15, 18, 28 | 🟠 Média-Alta | 6–8 semanas |
| **G** — Visualization & Marketplace | 4, 11, 21, 23 | 🟠 Média | 5–7 semanas |
| **Total** | 29 ideias | — | **40–54 semanas** |

---

## Dependências entre Waves

```
Wave A (Contract & Change Core)
    │
    ├──► Wave B (AI Ops) — usa contract health + confidence data
    ├──► Wave C (Intel) — knowledge graph agrega entidades de A
    ├──► Wave D (DevEx) — pipeline usa contracts de A
    └──► Wave G (Visual) — marketplace e simulador usam contracts de A

Wave B (AI Ops)
    └──► Wave E (Reliability) — self-healing usa narratives pattern de B

Wave C (Intel)
    └──► Wave E (Reliability) — maturity model alimenta chaos criteria

Wave F (FinOps) — relativamente independente, pode iniciar em paralelo
```

---

## Padrões de Implementação

Todas as ideias devem seguir os padrões já estabelecidos no NexTraceOne:

### Backend
- **Entidades:** `AuditableEntity` com `TenantId`, factory methods estáticos
- **Commands:** `ICommand<Response>` via MediatR, `Result<T>` para erros
- **Queries:** `IQuery<Response>` via MediatR
- **Repositories:** `IRepository<T>` + `IUnitOfWork`
- **Validação:** FluentValidation
- **Async:** `CancellationToken` em todas as operações
- **DateTime:** `IDateTimeProvider`, nunca `DateTime.Now`
- **Logging:** Serilog estruturado
- **RLS:** Toda nova tabela adicionada a `apply-rls.sql`
- **Migrations:** Nomeadas por fase (ex: `P09_ContractHealthScore`)

### Frontend
- **i18n:** Todo texto visível via chaves de tradução
- **State:** TanStack Query para server state, Zustand para UI state
- **Routing:** TanStack Router
- **Components:** Radix UI + Tailwind CSS
- **Charts:** Apache ECharts
- **Testes:** Vitest (unit) + Playwright (E2E)

### AI
- **Agentes:** Registados no `DefaultAgentCatalog`
- **Governança:** Token quota, audit trail, política de modelo
- **Grounding:** Dados do NexTraceOne via cross-module interfaces
- **Output:** Structured JSON para fitness/scoring, natural language para narrativas

---

## Próximos Passos

1. **Validar priorização** — confirmar ordem dos Waves
2. **Iniciar Wave A** — Contract Health Score como primeira implementação
3. **Preparar AI model** — instalar Qwen 2.5 32B para suportar Waves B+
4. **Definir sprints** — decompor cada ideia em tasks menores
5. **Alocar equipa** — backend + frontend + AI por Wave

---

> **Estado:** 🟢 Plano aprovado — pronto para iniciar implementação  
> **Referência:** [BRAINSTORMING-INNOVATIVE-IDEAS.md](./BRAINSTORMING-INNOVATIVE-IDEAS.md) | [AI-MODELS-ANALYSIS.md](./AI-MODELS-ANALYSIS.md)
