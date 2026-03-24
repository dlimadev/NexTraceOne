# Revisão Modular — AI Knowledge

> **Data:** 2026-03-24  
> **Prioridade:** P4 (Calibrar Expectativas)  
> **Módulo Backend:** `src/modules/aiknowledge/`  
> **Módulo Frontend:** `src/frontend/src/features/ai-hub/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **AI Knowledge** cobre toda a camada de inteligência artificial do NexTraceOne. Organizado em 4 subdomínios:

- **ExternalAI** — Providers de IA externa, knowledge queries
- **Governance** — Model registry, políticas de acesso, budgets, routing, IDE integrations
- **Runtime** — Execução de queries de IA
- **Orchestration** — Conversas, assistente, agentes, knowledge capture, test artifact generation

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Forte | IA é pilar oficial do produto |
| Completude funcional | ⚠️ **Parcial (~20-25%)** | Governance funcional, orchestration com stubs |
| Frontend | ⚠️ **Extensivo mas parcial** | 11 páginas, UI completa, backend parcial |
| **Problema principal** | 🔴 **Documentação excessivamente otimista** | 6+ documentos prometem sistema completo; realidade é 20-25% |
| Menu | ⚠️ **Sobre-representado** | 9 itens no menu para módulo parcial |

---

## 3. Páginas Frontend (11 páginas)

| Página | Rota | Permissão | Estado | Observação |
|--------|------|-----------|--------|------------|
| AiAssistantPage | `/ai/assistant` | ai:assistant:read | ⚠️ Parcial | 483 linhas — UI completa, backend com stubs de LLM |
| AiAgentsPage | `/ai/agents` | ai:assistant:read | ⚠️ Parcial | Lista de agentes |
| AgentDetailPage | `/ai/agents/:id` | ai:assistant:read | ⚠️ Parcial | Detalhe de agente |
| ModelRegistryPage | `/ai/models` | ai:governance:read | ⚠️ Parcial | Registo de modelos |
| AiPoliciesPage | `/ai/policies` | ai:governance:read | ⚠️ Parcial | Políticas de acesso |
| AiRoutingPage | `/ai/routing` | ai:governance:read | ⚠️ Parcial | Routing de queries |
| IdeIntegrationsPage | `/ai/ide` | ai:governance:read | ⚠️ **Preview** | Sem extensões reais VS Code/Visual Studio |
| TokenBudgetPage | `/ai/budgets` | ai:governance:read | ⚠️ Parcial | Gestão de budgets |
| AiAuditPage | `/ai/audit` | ai:governance:read | ⚠️ Parcial | Auditoria de uso |
| AiAnalysisPage | `/ai/analysis` | ai:runtime:write | ⚠️ Parcial | Análise assistida |
| AiIntegrationsConfigurationPage | `/platform/configuration/ai-integrations` | platform:admin:read | ✅ Funcional | 6 secções de configuração |

---

## 4. Backend — Subdomínios

### 4.1 External AI

| Endpoint Module | Propósito |
|----------------|-----------|
| ExternalAiEndpointModule | Providers externos, knowledge queries |

**DbContext:** ExternalAiDbContext
**Entidades:** ExternalAiProvider, AiProvider, AiSource

### 4.2 Governance

| Endpoint Module | Propósito |
|----------------|-----------|
| AiGovernanceEndpointModule | Models, sources, policies, budgets, audit, routing |
| AiIdeEndpointModule | IDE integrations |

**DbContext:** AiGovernanceDbContext
**Entidades:** AIAccessPolicy, AiTokenQuotaPolicy, AiContext

### 4.3 Runtime

| Endpoint Module | Propósito |
|----------------|-----------|
| AiRuntimeEndpointModule | Query execution |

### 4.4 Orchestration

| Endpoint Module | Propósito |
|----------------|-----------|
| AiOrchestrationEndpointModule | Conversations, agents, assistant, knowledge |

**DbContext:** AiOrchestrationDbContext
**Entidades:** AiConversation, KnowledgeCaptureEntry, GeneratedTestArtifact

---

## 5. Estado Real vs Documentação

| Documento | Promete | Realidade |
|-----------|---------|-----------|
| AI-ARCHITECTURE.md | Sistema multi-camada completo | ~20-25% implementado |
| AI-ASSISTED-OPERATIONS.md | 3 tipos de IA operacional | Implementação básica |
| AI-DEVELOPER-EXPERIENCE.md | IDE extensions (VS Code, Visual Studio) | Apenas página de UI, sem extensões reais |
| AI-GOVERNANCE.md | Framework completo de controle | Governance funcional, orchestration com stubs |
| AI-LOCAL-IMPLEMENTATION-AUDIT.md | Auditoria histórica | Marcado como referência histórica |

### Gap Principal

A documentação descreve um sistema de IA completo com:
- IA interna local como padrão
- Integração com múltiplos LLM providers
- IDE extensions reais
- Knowledge grounding completo

A realidade é:
- Governance funcional (modelos, políticas, budgets)
- Orchestration com stubs (conversas, agentes)
- Sem integração real com LLM SDKs
- Sem IDE extensions reais
- ~5 testes reais

---

## 6. Classificação das Funcionalidades

| Funcionalidade | Classificação | Justificativa |
|---------------|---------------|---------------|
| AI Governance (models, policies, budgets) | **Parcial** | Backend funcional, UI completa |
| AI Assistant (chat) | **Parcial/Preview** | UI completa (483 linhas), backend com stubs |
| AI Agents | **Parcial/Preview** | UI existe, backend parcial |
| AI Routing | **Preview** | UI existe, lógica básica |
| IDE Integrations | **Preview** | Apenas página de UI, sem extensões reais |
| Token Budget | **Parcial** | UI e configuração existem |
| AI Audit | **Parcial** | UI existe, dados limitados |
| AI Analysis | **Preview** | UI existe, dependente de LLM real |

---

## 7. Resumo de Ações

### Ações Críticas (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | **Calibrar documentação** — adicionar indicadores de maturidade real (%) em todos os docs de AI | 3h |
| 2 | **Definir MVP de AI** — o que é mínimo viável para produção | 4h |
| 3 | Decidir se IDE Integrations deve continuar no menu ou ser ocultada | 30 min |

### Ações de Validação (P2)

| # | Ação | Esforço |
|---|------|---------|
| 4 | Validar AI Governance flow (model registry → policies → budgets) | 2h |
| 5 | Validar AI Assistant (chat conversation flow) | 2h |
| 6 | Verificar se AiOrchestration tem endpoints reais ou stubs | 2h |

### Ações de Decisão (P3)

| # | Ação | Esforço |
|---|------|---------|
| 7 | Avaliar redução de 9 para 5-6 itens no menu (agrupar governance items) | 1h |
| 8 | Avaliar remoção/ocultação de IdeIntegrationsPage até existirem extensões reais | 30 min |
| 9 | Planear integração com LLM SDK real (OpenAI, Azure OpenAI, local models) | 8h |
| 10 | Incrementar testes (~5 existentes) | 4h |
