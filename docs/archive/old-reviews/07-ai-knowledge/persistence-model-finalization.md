# PARTE 6 — Persistência PostgreSQL Final

> **Módulo:** AI & Knowledge (07)
> **Prefixo:** `aik_`
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Tabelas atuais do módulo

### 1.1 AiGovernanceDbContext (19 DbSets, 7 migrations)

| DbSet | Tabela atual (estimada) | Entidade |
|-------|------------------------|----------|
| Models | `ai_models` | AIModel |
| Providers | `ai_providers` | AiProvider |
| Sources | `ai_sources` | AiSource |
| Agents | `ai_agents` | AiAgent |
| AgentExecutions | `ai_agent_executions` | AiAgentExecution |
| AgentArtifacts | `ai_agent_artifacts` | AiAgentArtifact |
| AccessPolicies | `ai_access_policies` | AIAccessPolicy |
| Budgets | `ai_budgets` | AIBudget |
| TokenQuotaPolicies | `ai_token_quota_policies` | AiTokenQuotaPolicy |
| TokenUsageLedger | `ai_token_usage_ledger` | AiTokenUsageLedger |
| Conversations | `ai_conversations` | AiAssistantConversation |
| Messages | `ai_messages` | AiMessage |
| UsageEntries | `ai_usage_entries` | AIUsageEntry |
| KnowledgeSources | `ai_knowledge_sources` | AIKnowledgeSource |
| IdeClientRegistrations | `ai_ide_client_registrations` | AIIDEClientRegistration |
| IdeCapabilityPolicies | `ai_ide_capability_policies` | AIIDECapabilityPolicy |
| RoutingDecisions | `ai_routing_decisions` | AIRoutingDecision |
| RoutingStrategies | `ai_routing_strategies` | AIRoutingStrategy |
| ExternalInferenceRecords | `ai_external_inference_records` | AiExternalInferenceRecord |

### 1.2 ExternalAiDbContext (4 DbSets, 1 migration)

| DbSet | Tabela atual (estimada) | Entidade |
|-------|------------------------|----------|
| Providers | `external_ai_providers` | ExternalAiProvider |
| Policies | `external_ai_policies` | ExternalAiPolicy |
| Consultations | `external_ai_consultations` | ExternalAiConsultation |
| KnowledgeCaptures | `knowledge_captures` | KnowledgeCapture |

### 1.3 AiOrchestrationDbContext (4 DbSets, 1 migration)

| DbSet | Tabela atual (estimada) | Entidade |
|-------|------------------------|----------|
| Contexts | `ai_contexts` | AiContext |
| Conversations | `ai_conversations` | AiConversation |
| TestArtifacts | `generated_test_artifacts` | GeneratedTestArtifact |
| KnowledgeCaptureEntries | `knowledge_capture_entries` | KnowledgeCaptureEntry |

---

## 2. Nomes finais com prefixo `aik_`

### 2.1 Tabelas principais (PostgreSQL)

| Entidade | Nome final | Justificação |
|----------|-----------|--------------|
| AIModel | `aik_models` | Registo de modelos LLM |
| AiProvider | `aik_providers` | Configuração de providers |
| AiAgent | `aik_agents` | Definição de agents |
| AiAgentExecution | `aik_agent_executions` | Log de execuções |
| AiAgentArtifact | `aik_agent_artifacts` | Artefactos gerados |
| AIAccessPolicy | `aik_access_policies` | Políticas de acesso |
| AIBudget | `aik_budgets` | Orçamentos |
| AiTokenQuotaPolicy | `aik_token_quota_policies` | Quotas de tokens |
| AiAssistantConversation | `aik_conversations` | Conversas do assistente |
| AiMessage | `aik_messages` | Mensagens de chat |
| AiSource | `aik_sources` | Fontes de conhecimento |
| AIIDEClientRegistration | `aik_ide_clients` | Clientes IDE (futuro) |
| AIIDECapabilityPolicy | `aik_ide_capabilities` | Capacidades IDE (futuro) |
| AIRoutingStrategy | `aik_routing_strategies` | Estratégias de routing |
| ExternalAiProvider | `aik_external_providers` | Providers externos |
| ExternalAiPolicy | `aik_external_policies` | Políticas externas |
| AiContext | `aik_orchestration_contexts` | Contextos de análise |
| GeneratedTestArtifact | `aik_test_artifacts` | Artefactos de teste |

### 2.2 Tabelas de log/audit (candidatas a ClickHouse futuro)

| Entidade | Nome PostgreSQL (mínimo) | Candidata ClickHouse |
|----------|-------------------------|---------------------|
| AiTokenUsageLedger | `aik_token_usage_ledger` | ✅ Alto volume |
| AIUsageEntry | `aik_usage_entries` | ✅ Alto volume |
| AIRoutingDecision | `aik_routing_decisions` | ✅ Alto volume |
| AIEnrichmentResult | `aik_enrichment_results` | ✅ Alto volume |
| AiExternalInferenceRecord | `aik_external_inferences` | ✅ Alto volume |
| ExternalAiConsultation | `aik_external_consultations` | ⚠️ Médio volume |
| KnowledgeCapture | `aik_knowledge_captures` | ❌ Baixo volume |

---

## 3. Definição de PKs, FKs, índices

### 3.1 PKs (todas Guid)

| Tabela | PK |
|--------|-----|
| `aik_models` | `Id` (Guid) |
| `aik_providers` | `Id` (Guid) |
| `aik_agents` | `Id` (Guid) |
| `aik_agent_executions` | `Id` (Guid) |
| `aik_agent_artifacts` | `Id` (Guid) |
| `aik_access_policies` | `Id` (Guid) |
| `aik_budgets` | `Id` (Guid) |
| `aik_token_quota_policies` | `Id` (Guid) |
| `aik_conversations` | `Id` (Guid) |
| `aik_messages` | `Id` (Guid) |
| `aik_sources` | `Id` (Guid) |
| (todas as restantes) | `Id` (Guid) |

### 3.2 FKs

| Tabela | FK | Referência |
|--------|----|-----------|
| `aik_messages` | `ConversationId` | `aik_conversations.Id` |
| `aik_agent_executions` | `AgentId` | `aik_agents.Id` |
| `aik_agent_artifacts` | `AgentId` | `aik_agents.Id` |
| `aik_agent_artifacts` | `ExecutionId` | `aik_agent_executions.Id` |
| `aik_token_quota_policies` | — | Sem FK (scope-based) |
| `aik_external_policies` | `ProviderId` | `aik_external_providers.Id` |
| `aik_usage_entries` | `ModelId` | `aik_models.Id` (soft) |
| `aik_token_usage_ledger` | `ModelId` | `aik_models.Id` (soft) |

### 3.3 Índices obrigatórios

| Tabela | Índice | Colunas |
|--------|--------|---------|
| `aik_models` | `IX_aik_models_tenant_status` | `TenantId, Status` |
| `aik_providers` | `IX_aik_providers_tenant_active` | `TenantId, IsActive` |
| `aik_agents` | `IX_aik_agents_tenant_status` | `TenantId, PublicationStatus` |
| `aik_agent_executions` | `IX_aik_executions_agent_date` | `AgentId, ExecutedAt` |
| `aik_conversations` | `IX_aik_conversations_tenant_user` | `TenantId, CreatedBy` |
| `aik_messages` | `IX_aik_messages_conversation` | `ConversationId, CreatedAt` |
| `aik_usage_entries` | `IX_aik_usage_tenant_date` | `TenantId, Timestamp` |
| `aik_token_usage_ledger` | `IX_aik_ledger_user_date` | `UserId, Timestamp` |
| `aik_access_policies` | `IX_aik_policies_scope` | `Scope, TenantId` |

---

## 4. Colunas de auditoria (via NexTraceDbContextBase)

Todas as tabelas herdam:
- `CreatedAt` (DateTimeOffset)
- `CreatedBy` (string)
- `UpdatedAt` (DateTimeOffset, nullable)
- `UpdatedBy` (string, nullable)
- `TenantId` (Guid)
- `IsDeleted` (bool, soft-delete)
- `RowVersion` (xmin para concorrência)

### Tabelas com EnvironmentId

| Tabela | EnvironmentId | Motivo |
|--------|---------------|--------|
| `aik_agent_executions` | ⚠️ Considerar | Execuções podem ser context-specific |
| `aik_orchestration_contexts` | ✅ Sim | Análises são environment-specific |
| Restantes | ❌ Não | Modelos/policies/agents são cross-environment |

---

## 5. O que realmente precisa existir no PostgreSQL

### 5.1 Obrigatório no PostgreSQL (dados transacionais)

| Tabela | Motivo |
|--------|--------|
| `aik_models` | Configuração — low volume, precisa de transações |
| `aik_providers` | Configuração — low volume |
| `aik_agents` | Definição — low volume |
| `aik_access_policies` | Segurança — precisa de consistência forte |
| `aik_budgets` | Controlo financeiro — precisa de consistência forte |
| `aik_token_quota_policies` | Controlo — precisa de consistência forte |
| `aik_conversations` | Metadados de conversa — low volume |
| `aik_messages` | Mensagens de chat — médio volume (PostgreSQL OK para MVP) |
| `aik_sources` | Configuração — low volume |
| `aik_ide_clients` | Configuração — low volume (futuro) |
| `aik_external_providers` | Configuração — low volume |
| `aik_external_policies` | Configuração — low volume |
| `aik_orchestration_contexts` | Contexto de análise — low volume |
| `aik_knowledge_captures` | Dados governados — low volume |
| `aik_routing_strategies` | Configuração — low volume |

### 5.2 Pode migrar para ClickHouse (futuro)

| Tabela | Volume esperado | Motivo |
|--------|----------------|--------|
| `aik_usage_entries` | Alto | Séries temporais de uso |
| `aik_token_usage_ledger` | Alto | Log de tokens |
| `aik_agent_executions` | Médio-alto | Log de execuções |
| `aik_routing_decisions` | Alto | Log de decisões |
| `aik_enrichment_results` | Médio | Log de enriquecimento |
| `aik_external_inferences` | Médio | Log de inferência externa |

---

## 6. Divergências entre estado atual e modelo final

| # | Divergência | Impacto |
|---|------------|---------|
| D-01 | Tabelas sem prefixo `aik_` | Todas as tabelas precisam ser renomeadas nas futuras migrations |
| D-02 | 3 DbContexts separados | Regra é 1 DbContext por módulo — necessário consolidar ou justificar exceção |
| D-03 | `AiConversation` duplicada | Orchestration tem AiConversation que duplica AiAssistantConversation |
| D-04 | `AiSource` vs `AIKnowledgeSource` | Duas entidades para conceito similar |
| D-05 | `KnowledgeCapture` vs `KnowledgeCaptureEntry` | Duas entidades para knowledge capture |
| D-06 | TenantId fixes em migrations | 2 migrations de correção (debt técnico) |
| D-07 | Sem RowVersion (xmin) confirmado | Necessário validar se NexTraceDbContextBase aplica xmin |
| D-08 | Índices podem estar incompletos | Necessário auditar migrations vs modelo ideal |

### D-02 Decisão: 3 DbContexts

**Recomendação:** Manter os 3 DbContexts como **exceção documentada**. O módulo é o mais complexo do produto com 30+ entidades. Consolidar para 1 DbContext criaria um contexto demasiado grande. A separação em Governance/ExternalAI/Orchestration é lógica e mantém bounded contexts internos.

**Alternativa:** Consolidar ExternalAI e Orchestration num 2º DbContext, mantendo Governance como principal. Total: 2 DbContexts.
