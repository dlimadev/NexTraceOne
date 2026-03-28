# Relatório de IA, Agentes e Governança de IA — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

IA no NexTraceOne é capacidade governada, não chat genérico. Deve incluir: modelo governado, auditoria de uso, controlo de acesso por persona/tenant/ambiente, e assistência real a decisões operacionais (contratos, mudanças, incidentes, runbooks).

---

## Estado Atual Encontrado

### Sumário de Estado

| Componente | Estado | Evidência |
|---|---|---|
| AI Governance (modelos, políticas, budgets) | ✅ REAL | `AiGovernanceDbContext`, model registry funcional |
| AI Access Policies | ✅ REAL | Políticas por tenant/role — real |
| AI Token & Budget Governance | ✅ REAL | `AiTokenUsageLedger` persistido |
| Model Registry | ⚠️ PARCIAL | Real mas não conectado ao routing real |
| AI Audit & Usage | ⚠️ PARCIAL | Real mas audita mock (assistant retorna hardcoded) |
| AI Tools (list_services, etc.) | ✅ REAL | 3 ferramentas reais confirmadas |
| AI Knowledge Sources / Context builders | ⚠️ PARCIAL | Estrutura real, sem retrieval/RAG real |
| AI Assistant (SendAssistantMessage) | ❌ BROKEN | Retorna respostas hardcoded — sem LLM real E2E |
| AiAssistantPage | ❌ MOCK | `mockConversations` hardcoded |
| AI Agents (orchestration) | ⚠️ PARCIAL | State machines reais; orchestration parcialmente implementada |
| External AI Integrations | ❌ PLAN | 8 handlers TODO stub; `ExternalAiModule` é stub |
| IDE Extensions Management | ⚠️ PARCIAL | `AiIdeEndpointModule` existe; sem validação E2E de extensão IDE |

---

## AIKnowledge Module — Análise por SubContexto

`src/modules/aiknowledge/` | 287 ficheiros | 3 DbContexts | 11 migrações

### 1. AI Governance (`AiGovernanceDbContext`) — READY
**Migrações:** 3

**O que está real e funcional:**
- `AiProvider`: registo de providers (Ollama, OpenAI, etc.) com configuração por tenant
- `AiModel`: registry de modelos com capabilities, contexto máximo, pricing
- `AiAccessPolicy`: políticas de acesso por role/tenant/ambiente
- `AiAgentDefinition`: definição de agentes com estado (Draft → Active → Published → Archived)
- `AiTokenUsageLedger`: contabilização real de tokens por utilizador/agente/tenant
- `AiAuditEntry`: auditoria de uso de IA
- `AiKnowledgeSource`: fontes de conhecimento configuradas

**Verificação:** Todas as entidades têm DbSets, migrations, e handlers reais que consultam o DbContext.

### 2. AI Orchestration (`AiOrchestrationDbContext`) — PARCIAL
**Migrações:** 5

**O que existe:**
- `AiAssistantConversation`: conversações de assistente com mensagens
- `AiMessage`: mensagens de conversa com role e conteúdo
- `AiAgentExecution`: execuções de agentes com estado
- State machine real para `AgentPublicationStatus`

**O que está quebrado:**
- `SendAssistantMessage` handler (256 linhas) tem lógica de routing, grounding e contexto real, **mas retorna resposta hardcoded** em vez de chamar o LLM
- `IExternalAIRoutingPort` existe como abstração — implementação real para Ollama não está conectada
- Outbox sem processador registado para `AiOrchestrationDbContext`

### 3. External AI (`ExternalAiDbContext`) — PLAN
**Migrações:** 3

**Estado:** `ExternalAiModule.cs` implementa `IExternalAiModule` mas todos os 8 métodos têm `TODO: Phase 03.x`. Sem lógica real de chamada a LLM externo.

**Ficheiro chave:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Services/ExternalAiModule.cs`

---

## SendAssistantMessage — Análise Detalhada

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Features/SendAssistantMessage/`

O handler tem 256 linhas com lógica real de:
- routing intelligence (provider selection)
- context building (grounding com contratos, mudanças, serviços)
- `AiTokenUsageLedger` update
- `AiAuditEntry` creation
- `AiMessage` persistence

**Mas:** a chamada real ao LLM não acontece. A resposta é hardcoded como texto de fallback. `Ollama` está configurado em `appsettings.json` (`http://localhost:11434`) mas a chamada HTTP não chega ao provider.

**Gap:** Conectar `IExternalAIRoutingPort` ao provider Ollama implementado é a ação que fecha este fluxo.

**TODOs confirmados (aiknowledge):**
- 3 em Orchestration (Phase 02.6 — metadata de atribuição de token/modelo)
- 6 em ExternalAI (Phase 03.x — knowledge capture, external routing, retrieval)

---

## AI Tools — 3 Ferramentas Reais

Confirmadas como reais no domain:
1. `list_services` — lista serviços do catalog
2. `get_service_health` — saúde de serviço via OperationalIntelligence
3. `list_recent_changes` — mudanças recentes via ChangeGovernance

**Gap:** Ferramentas existem no domain mas `SendAssistantMessage` não chega a invocá-las por causa do hardcoded response.

---

## AiAssistantPage — Frontend MOCK

**Ficheiro:** `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`

Confirmado por inspeção: contém `mockConversations` hardcoded. Não consume a API real de `AiOrchestrationDbContext`.

---

## Governance de IA — Verificação de Requisitos CLAUDE.md

| Requisito | Estado | Evidência |
|---|---|---|
| Contexto do produto | ✅ | Context builders com contratos, mudanças, serviços |
| Política de acesso | ✅ | `AiAccessPolicy` real |
| Auditabilidade | ⚠️ PARCIAL | `AiAuditEntry` real mas audita resposta hardcoded |
| Observabilidade de uso | ✅ | `AiTokenUsageLedger` |
| Governança de modelo | ✅ | Model registry real |
| i18n na UI | ✅ | i18n aplicado |
| Tenant awareness | ✅ | Tenant filtrado em todas as queries |
| Environment awareness | ⚠️ | Presente na estrutura; verificar em fluxo real |
| Persona awareness | ⚠️ | Estrutura existe; sem validação E2E |
| Controlo de dados para modelos externos | ✅ | OpenAI disabled por default; política de acesso |

---

## Avaliação por Caso de Uso de IA (CLAUDE.md §11.3)

### IA Operacional
| Caso | Estado |
|---|---|
| Investigar problema em produção | ❌ — assistant não funciona E2E |
| Correlacionar incidente com mudança | ❌ — engine de correlação ausente |
| Sugerir causa provável | ❌ — sem LLM real |
| Recomendar mitigação | ❌ — sem LLM real |
| Consultar telemetry, changes, topology, contracts | ⚠️ — context builders existem; não testados E2E |

### IA de Engenharia
| Caso | Estado |
|---|---|
| Gerar contratos REST/SOAP/AsyncAPI | ❌ — sem LLM real |
| Sugerir schemas e exemplos | ❌ — sem LLM real |
| Validar compatibilidade | ⚠️ — sem LLM real (lógica de domain existe sem AI) |
| Acelerar desenvolvimento com contexto governado | ⚠️ — context builders existem |

### IA Governada
| Caso | Estado |
|---|---|
| Controlar quem pode usar qual modelo | ✅ REAL |
| Controlar dados que saem para modelos externos | ✅ OpenAI disabled; policy real |
| Budget de tokens | ✅ REAL |
| Auditar uso completo | ⚠️ PARCIAL — audita mock |

---

## Agentes Especializados — Estado

| Agente | Estado |
|---|---|
| Agente de criação de contrato REST | ❌ PLAN — sem LLM real |
| Agente de análise de change impact | ❌ PLAN |
| Agente de investigação operacional | ❌ PLAN |
| Agente de geração de cenários de teste | ❌ PLAN |
| Criação de agentes por utilizador | ⚠️ PARCIAL — `AiAgentDefinition` existe |

---

## Gaps Críticos

1. **AI Assistant sem LLM real** — `SendAssistantMessage` retorna hardcoded; `AiAssistantPage` usa mock
2. **ExternalAI 8 handlers TODO** — Phase 03.x não iniciado
3. **Outbox sem processador** para `AiOrchestrationDbContext`
4. **RAG/Knowledge retrieval** ausente — context builders sem retrieval semântico real
5. **IDE Extensions** — `AiIdeEndpointModule` existe mas extensão IDE não validada

---

## Recomendações

1. **Crítico:** Conectar `SendAssistantMessage` → `IExternalAIRoutingPort` → Ollama provider
2. **Crítico:** Remover `mockConversations` de `AssistantPanel.tsx` e conectar à API real
3. **Alta:** Implementar os 8 handlers ExternalAI (Phase 03.x)
4. **Alta:** Ativar outbox para `AiOrchestrationDbContext`
5. **Média:** Implementar RAG básico para knowledge retrieval do `KnowledgeDbContext`
6. **Média:** Conectar `AiAuditEntry` ao fluxo real (vai ficar automático após passo 1)

---

*Data: 28 de Março de 2026*
