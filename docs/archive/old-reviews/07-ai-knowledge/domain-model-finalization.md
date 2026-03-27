# PARTE 5 — Modelo de Domínio Final

> **Módulo:** AI & Knowledge (07)
> **Prefixo:** `aik_`
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Aggregate Roots

| Aggregate Root | Subdomínio | Ficheiro | LOC | Estado |
|---------------|------------|----------|-----|--------|
| `AIModel` | Governance | `Domain/Governance/Entities/AIModel.cs` | ~120 | ✅ Rico |
| `AiProvider` | Governance | `Domain/Governance/Entities/AiProvider.cs` | ~80 | ⚠️ Pode necessitar health state |
| `AiAgent` | Governance | `Domain/Governance/Entities/AiAgent.cs` | ~150 | ⚠️ AllowedTools sem execução |
| `AIAccessPolicy` | Governance | `Domain/Governance/Entities/AIAccessPolicy.cs` | ~80 | ✅ Funcional |
| `AIBudget` | Governance | `Domain/Governance/Entities/AIBudget.cs` | ~60 | ⚠️ Sem enforcement |
| `AiAssistantConversation` | Governance | `Domain/Governance/Entities/AiAssistantConversation.cs` | ~80 | ✅ Funcional |
| `ExternalAiProvider` | ExternalAI | `Domain/ExternalAI/Entities/ExternalAiProvider.cs` | ~60 | ⚠️ Parcial |
| `AiContext` | Orchestration | `Domain/Orchestration/Entities/AiContext.cs` | ~60 | ⚠️ Parcial |

---

## 2. Entidades (não-root)

| Entidade | Aggregate Parent | Ficheiro | Estado |
|----------|-----------------|----------|--------|
| `AiMessage` | AiAssistantConversation | `Domain/Governance/Entities/AiMessage.cs` | ✅ |
| `AiAgentExecution` | AiAgent | `Domain/Governance/Entities/AiAgentExecution.cs` | ⚠️ Parcial |
| `AiAgentArtifact` | AiAgent | `Domain/Governance/Entities/AiAgentArtifact.cs` | ⚠️ Parcial |
| `AiTokenUsageLedger` | — (standalone) | `Domain/Governance/Entities/AiTokenUsageLedger.cs` | ⚠️ |
| `AiTokenQuotaPolicy` | — (standalone) | `Domain/Governance/Entities/AiTokenQuotaPolicy.cs` | ⚠️ |
| `AIUsageEntry` | — (standalone) | `Domain/Governance/Entities/AIUsageEntry.cs` | ⚠️ |
| `AIKnowledgeSource` | — (standalone) | `Domain/Governance/Entities/AIKnowledgeSource.cs` | ⚠️ |
| `AiSource` | — (standalone) | `Domain/Governance/Entities/AiSource.cs` | ⚠️ |
| `AIIDEClientRegistration` | — (standalone) | `Domain/Governance/Entities/AIIDEClientRegistration.cs` | ❌ UI-only |
| `AIIDECapabilityPolicy` | — (standalone) | `Domain/Governance/Entities/AIIDECapabilityPolicy.cs` | ❌ UI-only |
| `AIRoutingDecision` | — (standalone) | `Domain/Governance/Entities/AIRoutingDecision.cs` | ⚠️ |
| `AIRoutingStrategy` | — (standalone) | `Domain/Governance/Entities/AIRoutingStrategy.cs` | ⚠️ |
| `AIEnrichmentResult` | — (standalone) | `Domain/Governance/Entities/AIEnrichmentResult.cs` | ⚠️ |
| `AiExternalInferenceRecord` | — (standalone) | `Domain/Governance/Entities/AiExternalInferenceRecord.cs` | ⚠️ |
| `AIExecutionPlan` | — (standalone) | `Domain/Governance/Entities/AIExecutionPlan.cs` | ⚠️ |
| `ExternalAiPolicy` | ExternalAiProvider | `Domain/ExternalAI/Entities/ExternalAiPolicy.cs` | ⚠️ |
| `ExternalAiConsultation` | — (standalone) | `Domain/ExternalAI/Entities/ExternalAiConsultation.cs` | ⚠️ |
| `KnowledgeCapture` | — (standalone) | `Domain/ExternalAI/Entities/KnowledgeCapture.cs` | ⚠️ |
| `AiConversation` | — (standalone) | `Domain/Orchestration/Entities/AiConversation.cs` | ⚠️ Duplicação com Governance? |
| `GeneratedTestArtifact` | — (standalone) | `Domain/Orchestration/Entities/GeneratedTestArtifact.cs` | ⚠️ |
| `KnowledgeCaptureEntry` | — (standalone) | `Domain/Orchestration/Entities/KnowledgeCaptureEntry.cs` | ⚠️ |

---

## 3. Enums persistidos

| Enum | Valores | Ficheiro |
|------|---------|----------|
| `ModelStatus` | Registered, Active, Inactive, Deprecated, Failed | `Governance/Enums/` |
| `ModelType` | EmbeddingModel, ChatCompletion, TextCompletion, ImageGeneration | `Governance/Enums/` |
| `AgentOwnershipType` | System, Tenant, User | `Governance/Enums/` |
| `AgentPublicationStatus` | Draft, PendingReview, Active, Published, Archived, Blocked | `Governance/Enums/` |
| `AgentVisibility` | Private, Team, Tenant, Public | `Governance/Enums/` |
| `AgentExecutionStatus` | Pending, Running, Success, FailedWithFallback, Failed, TimedOut | `Governance/Enums/` |
| `AgentArtifactType` | TestScenario, ReleaseNotes, RobotFramework, PromptTemplate, CodeGeneration | `Governance/Enums/` |
| `AIClientType` | WebApplication, VSCode, VisualStudio, JetBrains, CLI | `Governance/Enums/` |
| `AIIDECommandType` | CodeCompletion, Refactoring, Explanation, Testing, Documentation | `Governance/Enums/` |
| `KnowledgeSourceType` | ServiceCatalog, ContractLibrary, IncidentDatabase, ChangeLog, Runbooks, Telemetry | `Governance/Enums/` |
| `AIUseCaseType` | ServiceAnalysis, ChangeClassification, IncidentInvestigation, ReleasePlanning | `Governance/Enums/` |
| `AISourceRelevance` | High, Medium, Low | `Governance/Enums/` |
| `AIConfidenceLevel` | VeryHigh, High, Medium, Low, Unknown | `Governance/Enums/` |
| `AIEscalationReason` | Uncertainty, Contradiction, MissingData, UserRequest | `Governance/Enums/` |
| `AIRoutingPath` | Internal, External, Hybrid | `Governance/Enums/` |
| `BudgetPeriod` | Monthly, Quarterly, Annual, Unlimited | `Governance/Enums/` |
| `QuotaLimitType` | TokenCount, RequestCount, CostAmount | `Governance/Enums/` |
| `ProviderHealthStatus` | Healthy, Degraded, Unavailable, Unknown | `Governance/Enums/` |
| `UsageResult` | Success, PartialSuccess, Failed, Fallback | `Governance/Enums/` |
| `AuthenticationMode` | ApiKey, OAuth2, MutualTLS, ServiceAccount | `Governance/Enums/` |
| `ConsultationStatus` | Pending, Completed, Failed, Archived | `ExternalAI/Enums/` |
| `KnowledgeStatus` | Captured, Reviewed, Approved, Rejected, Archived | `ExternalAI/Enums/` |
| `AiSourceType` | Internal, External, Hybrid | `Governance/Enums/` |

---

## 4. Problemas identificados

### 4.1 Entidades anémicas

| Entidade | Problema |
|----------|----------|
| `AIUsageEntry` | Provavelmente apenas propriedades — sem regras de negócio |
| `AiTokenUsageLedger` | Append-only log — aceitável como entidade simples |
| `AIRoutingDecision` | Log de decisão — aceitável |
| `AIEnrichmentResult` | Resultado de operação — sem lógica de domínio |
| `AiExternalInferenceRecord` | Log de inferência externa — sem lógica |

### 4.2 Regras de negócio fora do lugar

| Regra | Onde está | Onde deveria estar |
|-------|-----------|-------------------|
| Resolução de modelo para chat | Provavelmente no handler | Deveria ser `AIModel.CanBeUsedBy(userId, scope)` |
| Validação de AllowedTools | Ausente | `AiAgent.ValidateToolExecution(toolName)` |
| Enforcement de quota | Ausente | `AiTokenQuotaPolicy.IsWithinQuota(currentUsage)` |
| Validação de orçamento | Ausente | `AIBudget.CanExecute(estimatedTokens)` |

### 4.3 Campos ausentes

| Entidade | Campo ausente | Motivo |
|----------|--------------|--------|
| `AiAgent` | `MaxTokensPerExecution` | Limitar custo por execução |
| `AiAgent` | `TimeoutSeconds` | Controlo de timeout |
| `AiAgent` | `RequiresHumanApproval` | Human-in-the-loop |
| `AiProvider` | `LastHealthCheckAt` | Rastreabilidade de health |
| `AiProvider` | `RateLimitPerMinute` | Rate limiting |
| `AiAgentExecution` | `ErrorDetails` | Detalhes de erro estruturados |
| `AiMessage` | `ContextSourceIds` | Rastreabilidade de retrieval |

### 4.4 Campos indevidos / duplicação

| Entidade | Problema |
|----------|----------|
| `AiConversation` (Orchestration) | ⚠️ Possível duplicação com `AiAssistantConversation` (Governance) |
| `AiSource` vs `AIKnowledgeSource` | ⚠️ Duas entidades para conceito similar — consolidar |
| `KnowledgeCapture` (ExternalAI) vs `KnowledgeCaptureEntry` (Orchestration) | ⚠️ Possível duplicação |

---

## 5. Relações com outros módulos

| Relação | Tipo | Interface |
|---------|------|-----------|
| Identity & Access → AI & Knowledge | UserId, TenantId via JWT | Claims no handler |
| AI & Knowledge → Catalog | Query de serviços para grounding | Integration query |
| AI & Knowledge → Change Governance | Dados de changes para classificação | Integration query |
| AI & Knowledge → Operational Intelligence | Dados de incidentes para investigação | Integration query |
| AI & Knowledge → Audit & Compliance | Emite eventos de uso de IA | Domain events (futuro) |
| AI & Knowledge → Notifications | Emite alertas de orçamento (futuro) | Domain events (futuro) |

---

## 6. Modelo final proposto

### Governance (consolidado)
- **AIModel** (AR) → AiProvider, AIRoutingStrategy
- **AiAgent** (AR) → AiAgentExecution → AiAgentArtifact
- **AIAccessPolicy** (AR)
- **AIBudget** (AR) → AiTokenQuotaPolicy
- **AiAssistantConversation** (AR) → AiMessage
- **AiSource** (consolidar com AIKnowledgeSource)
- **AIIDEClientRegistration** (AR) — manter mas marcar como futuro
- **AIUsageEntry** (log entity)
- **AiTokenUsageLedger** (log entity)
- **AIRoutingDecision** (log entity)
- **AIEnrichmentResult** (log entity)
- **AiExternalInferenceRecord** (log entity)

### ExternalAI
- **ExternalAiProvider** (AR) → ExternalAiPolicy
- **ExternalAiConsultation** (log entity)
- **KnowledgeCapture** (AR) — consolidar com KnowledgeCaptureEntry

### Orchestration
- **AiContext** (AR) — contexto de análise
- **GeneratedTestArtifact** (entity)
- Remover **AiConversation** — usar AiAssistantConversation de Governance
- Consolidar **KnowledgeCaptureEntry** → KnowledgeCapture (ExternalAI)
