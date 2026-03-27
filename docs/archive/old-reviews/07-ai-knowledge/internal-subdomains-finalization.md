# PARTE 2 — Estrutura Interna de Subdomínios

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Visão geral dos subdomínios

O módulo AI & Knowledge organiza-se em **4 subdomínios internos**, cada um com DbContext próprio (Governance) ou partilhado:

```
AI & Knowledge
├── Governance (AI Core)   — Modelos, providers, políticas, agents, audit
├── Runtime                — Execução de chat, inferência, health
├── Orchestration          — Análise assistida, geração, contexto
└── ExternalAI             — Providers externos, knowledge capture
```

---

## 2. Subdomínio: Governance (AI Core)

### 2.1 Responsabilidades

| Responsabilidade | Entidades | Estado |
|------------------|-----------|--------|
| Registo de modelos LLM | `AIModel` | ⚠️ Schema OK, registo frontend desativado |
| Registo de providers | `AiProvider` | ⚠️ Schema OK, health check parcial |
| Definição de agents | `AiAgent`, `AiAgentExecution`, `AiAgentArtifact` | ⚠️ Definição OK, tools NÃO executam |
| Políticas de acesso | `AIAccessPolicy` | ✅ Funcional |
| Orçamentos e quotas | `AIBudget`, `AiTokenQuotaPolicy`, `AiTokenUsageLedger` | ⚠️ Schema OK, enforcement parcial |
| Conversas e mensagens | `AiAssistantConversation`, `AiMessage` | ✅ Funcional |
| Registo de uso | `AIUsageEntry`, `AiExternalInferenceRecord` | ⚠️ Parcial |
| Sources de conhecimento | `AIKnowledgeSource`, `AiSource` | ⚠️ Schema OK, retrieval incerto |
| IDE clients | `AIIDEClientRegistration`, `AIIDECapabilityPolicy` | ❌ UI-only |
| Routing | `AIRoutingDecision`, `AIRoutingStrategy` | ⚠️ Schema OK, execução incerta |
| Enrichment | `AIEnrichmentResult` | ⚠️ Parcial |
| Execution planning | `AIExecutionPlan` | ⚠️ Schema existe, uso incerto |

### 2.2 DbContext

- **AiGovernanceDbContext** — 19 DbSets
- **Herança:** NexTraceDbContextBase (RLS, Audit, Outbox, Encryption)
- **Migrations:** 7 (com debt de TenantId fixes)

### 2.3 Features CQRS: 41

### 2.4 Endpoints: ~35 (AiGovernanceEndpointModule + AiIdeEndpointModule)

---

## 3. Subdomínio: Runtime

### 3.1 Responsabilidades

| Responsabilidade | Estado |
|------------------|--------|
| Execução de chat (inferência LLM) | ⚠️ Funcional parcial — streaming NÃO implementado |
| Health check de providers | ⚠️ Parcial |
| Registo de token usage | ⚠️ Parcial |
| Ativação/desativação de modelos | ⚠️ Schema OK |
| Pesquisa de dados/documentos/telemetria | ❌ Provável stub |

### 3.2 Features CQRS: ~7-10

Ficheiros:
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/`
- `ExecuteAiChat`, `ActivateModel`, `ListAiModels`, `ListAiProviders`, `ListAiSources`
- `CheckAiProvidersHealth`, `RecordExternalInference`, `GetTokenUsage`, `ListTokenPolicies`
- `SearchData`, `SearchDocuments`, `SearchTelemetry`

### 3.3 Endpoints: AiRuntimeEndpointModule

### 3.4 Nota crítica

O Runtime **NÃO tem DbContext próprio** — partilha o AiGovernanceDbContext para persistência de resultados de chat e token usage. Isto é aceitável para MVP mas pode necessitar de separação futura.

---

## 4. Subdomínio: Orchestration

### 4.1 Responsabilidades

| Responsabilidade | Estado |
|------------------|--------|
| Geração de test scenarios | ⚠️ Feature existe, qualidade incerta |
| Geração de Robot Framework drafts | ⚠️ Feature existe, qualidade incerta |
| Perguntas sobre catálogo | ⚠️ Feature existe, grounding incerto |
| Classificação AI de changes | ⚠️ Feature existe |
| Comparação de ambientes | ⚠️ Feature existe |
| Análise de ambientes non-prod | ⚠️ Feature existe |
| Validação de knowledge capture | ⚠️ Feature existe |
| Histórico de conversas AI | ⚠️ Feature existe |
| Avaliação de promoção | ⚠️ Feature existe |
| Sugestão de versão semântica | ⚠️ Feature existe |
| Resumo de release para aprovação | ⚠️ Feature existe |

### 4.2 DbContext

- **AiOrchestrationDbContext** — 4 DbSets
- Entidades: `AiContext`, `AiConversation`, `GeneratedTestArtifact`, `KnowledgeCaptureEntry`
- **Migrations:** 1 (InitialCreate)

### 4.3 Features CQRS: 12

### 4.4 Nota crítica

Muitas das features de Orchestration são **chamadas cross-module** (analisam dados de Catalog, Change Governance, Operational Intelligence). A maioria é provavelmente **stub ou parcial** — precisam de validação ponta a ponta.

---

## 5. Subdomínio: ExternalAI

### 5.1 Responsabilidades

| Responsabilidade | Estado |
|------------------|--------|
| Configuração de providers externos | ⚠️ Schema OK |
| Políticas de uso externo | ⚠️ Schema OK |
| Execução de consultas externas | ⚠️ Feature existe |
| Captura de conhecimento | ⚠️ Feature existe |
| Aprovação de knowledge | ⚠️ Feature existe |
| Reutilização de knowledge | ⚠️ Feature existe |

### 5.2 DbContext

- **ExternalAiDbContext** — 4 DbSets
- Entidades: `ExternalAiProvider`, `ExternalAiPolicy`, `ExternalAiConsultation`, `KnowledgeCapture`
- **Migrations:** 1 (InitialCreate)

### 5.3 Features CQRS: 8

---

## 6. Regras de não-duplicação entre subdomínios

| Aspecto | Onde vive | Onde NÃO deve ser duplicado |
|---------|-----------|----------------------------|
| Definição de modelos | Governance | Runtime NÃO redefine modelos |
| Definição de agents | Governance | Orchestration NÃO redefine agents |
| Políticas de acesso | Governance | ExternalAI NÃO redefine políticas gerais |
| Execução de inferência | Runtime | Governance NÃO executa inferência diretamente |
| Contexto de análise | Orchestration | Governance NÃO monta contexto de análise |
| Providers externos | ExternalAI | Governance gere providers internos, ExternalAI gere externos |
| Knowledge capture | ExternalAI | Orchestration pode gerar KnowledgeCaptureEntry, mas aprovação é de ExternalAI |

⚠️ **Conflito detetado:** Existem entidades de `Provider` e `AiSource` tanto em Governance como em ExternalAI. Necessário resolver na PARTE 5 (Domain Model).

---

## 7. Dependências entre subdomínios

```
Governance ──────► Runtime    (modelos, providers, políticas → execução)
Governance ──────► Orchestration (agents, modelos → análise)
Governance ──────► ExternalAI (políticas → consultas externas)
Runtime ─────────► Governance (resultados → audit, token usage)
Orchestration ───► Runtime    (inferência necessária para análise)
ExternalAI ──────► Governance (knowledge → knowledge sources)
```

---

## 8. Mapeamento para a nomenclatura do prompt

O prompt N13 refere-se a 3 subdomínios: **AI Core**, **Agents**, **Knowledge**.

| Nomenclatura N13 | Subdomínio real no código | Entidades principais |
|-------------------|--------------------------|---------------------|
| AI Core | Governance + Runtime | AIModel, AiProvider, AIAccessPolicy, AIBudget, AiAssistantConversation |
| Agents | Governance (sub-aggregate) | AiAgent, AiAgentExecution, AiAgentArtifact, AIExecutionPlan |
| Knowledge | ExternalAI + Orchestration (parcial) | AIKnowledgeSource, AiSource, KnowledgeCapture, KnowledgeCaptureEntry |

**Decisão:** Manter a organização real em 4 subdomínios (Governance, Runtime, Orchestration, ExternalAI) porque reflete o código. A visão conceitual AI Core / Agents / Knowledge mapeia-se transversalmente.

---

## 9. Resumo

| Subdomínio | Entidades | Features | DbContext | Migrations | Maturidade |
|------------|-----------|----------|-----------|------------|------------|
| Governance | 22 | 41 | AiGovernanceDbContext (19 DbSets) | 7 | 🟠 40% |
| Runtime | 0 próprias | 7-10 | Partilha Governance | 0 | 🔴 20% |
| Orchestration | 4 | 12 | AiOrchestrationDbContext (4 DbSets) | 1 | 🔴 15% |
| ExternalAI | 4 | 8 | ExternalAiDbContext (4 DbSets) | 1 | 🔴 20% |
| **Total** | **30+** | **68+** | **3** | **9** | **🔴 ~25%** |
