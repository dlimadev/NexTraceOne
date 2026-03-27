# PARTE 4 — Validação dos Fluxos Ponta a Ponta

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Fluxo: Chat de IA (AI Assistant)

### 1.1 Fluxo esperado

```
Utilizador → Frontend (AiAssistantPage) → POST /api/v1/ai/assistant/chat
  → SendAssistantMessage handler
    → Resolve modelo/provider
    → Monta contexto (retrieval, memória)
    → Executa inferência (LLM call)
    → Persiste AiMessage (role: user + assistant)
    → Persiste AIUsageEntry (tokens)
    → Retorna resposta
  → Frontend exibe resposta
```

### 1.2 Estado real

| Etapa | Estado | Ficheiro | Problema |
|-------|--------|----------|----------|
| Frontend envia mensagem | ✅ | `AiAssistantPage.tsx` → `sendMessage()` | — |
| Handler recebe pedido | ✅ | `SendAssistantMessage.cs` | — |
| Resolve modelo/provider | ⚠️ | Handler interno | Routing strategy pode ser stub |
| Monta contexto | ⚠️ | Serviço de contexto | Retrieval real incerto — pode montar prompt fixo |
| Executa inferência | ⚠️ | Runtime service | Sem streaming; provider call pode ser parcial |
| Persiste mensagem | ✅ | `AiMessage` via AiGovernanceDbContext | — |
| Persiste uso de tokens | ⚠️ | `AIUsageEntry` | Registo pode não acontecer em todos os cenários |
| Retorna resposta | ✅ | HTTP 200 com conteúdo | — |
| Frontend exibe | ✅ | AssistantPanel.tsx | ⚠️ AssistantPanel tem gerador de mock response |

### 1.3 Lacunas críticas

- ❌ **Streaming não implementado** — resposta é síncrona, sem SSE/WebSocket
- ⚠️ **AssistantPanel.tsx contém gerador de respostas mock** — pode mascarar falhas reais
- ⚠️ **Montagem de contexto** — não confirmado se retrieval é real ou template fixo
- ⚠️ **Token usage** — pode não registar em cenários de erro

---

## 2. Fluxo: Execução de Agent

### 2.1 Fluxo esperado

```
Utilizador → Frontend (AgentDetailPage) → POST /api/v1/ai/agents/{id}/execute
  → ExecuteAgent handler
    → Carrega definição do agent (AiAgent)
    → Resolve modelo
    → Monta contexto + system prompt
    → Executa inferência
    → Despacha tools (se AllowedTools definidos)
    → Persiste AiAgentExecution (status, output, tokens)
    → Gera artefactos (AiAgentArtifact) se aplicável
    → Retorna resultado
  → Frontend exibe resultado + artefactos
```

### 2.2 Estado real

| Etapa | Estado | Problema |
|-------|--------|----------|
| Frontend invoca execução | ✅ | `executeAgent()` API call |
| Handler recebe pedido | ✅ | `ExecuteAgent.cs` |
| Carrega definição | ✅ | Query AiAgent por ID |
| Resolve modelo | ⚠️ | Pode usar modelo padrão sem routing |
| Monta contexto | ⚠️ | System prompt do agent usado; contexto adicional incerto |
| Executa inferência | ⚠️ | LLM call parcial |
| **Despacha tools** | ❌ | **AllowedTools declarados mas NUNCA executados** |
| Persiste execução | ⚠️ | AiAgentExecution criado; pode faltar dados completos |
| Gera artefactos | ⚠️ | Pode criar AiAgentArtifact; qualidade incerta |
| Retorna resultado | ✅ | HTTP response |

### 2.3 Lacunas críticas

- 🔴 **Tools NÃO executam** — CR-2 do consolidated review. É o problema mais grave do módulo.
- ⚠️ **Sem framework de tool calling** — não existe mecanismo de despacho, validação ou sandbox
- ⚠️ **Sem human-in-the-loop** — agent executa sem aprovação humana

---

## 3. Fluxo: Orchestration (Análise cross-module)

### 3.1 Fluxo esperado (exemplo: ClassifyChangeWithAI)

```
Módulo Change Governance → POST /api/v1/ai/orchestration/classify-change
  → ClassifyChangeWithAI handler
    → Carrega dados do change (via integração)
    → Monta contexto com dados de catálogo + contratos
    → Executa inferência
    → Retorna classificação + confiança
    → Persiste decisão
```

### 3.2 Estado real

| Etapa | Estado | Problema |
|-------|--------|----------|
| Endpoint existe | ✅ | AiOrchestrationEndpointModule |
| Handler existe | ✅ | `ClassifyChangeWithAI.cs` |
| Carrega dados do change | ⚠️ | Depende de integração cross-module — pode ser stub |
| Monta contexto | ⚠️ | Grounding real incerto |
| Executa inferência | ⚠️ | LLM call — sem confirmação ponta a ponta |
| Retorna resultado | ⚠️ | Formato de resposta pode ser incompleto |
| Persiste decisão | ⚠️ | AiOrchestrationDbContext — pode não persistir |

### 3.3 Features de Orchestration — análise individual

| Feature | Estado estimado |
|---------|----------------|
| `GenerateTestScenarios` | ⚠️ Parcial — LLM call provável, qualidade incerta |
| `GenerateRobotFrameworkDraft` | ⚠️ Parcial — template-based + LLM |
| `AskCatalogQuestion` | ⚠️ Parcial — depende de retrieval funcional |
| `ClassifyChangeWithAI` | ⚠️ Parcial — depende de dados de Change Governance |
| `CompareEnvironments` | ⚠️ Parcial — depende de dados de Environment |
| `AnalyzeNonProdEnvironment` | ⚠️ Parcial |
| `ValidateKnowledgeCapture` | ⚠️ Parcial |
| `GetAiConversationHistory` | ✅ Query simples |
| `AssessPromotionReadiness` | ⚠️ Parcial — depende de múltiplos módulos |
| `SuggestSemanticVersionWithAI` | ⚠️ Parcial |
| `SummarizeReleaseForApproval` | ⚠️ Parcial |

---

## 4. Fluxo: External AI Consultation

### 4.1 Fluxo esperado

```
Utilizador → POST /api/v1/ai/external/query
  → QueryExternalAISimple/Advanced handler
    → Valida política de acesso externo
    → Seleciona provider externo
    → Executa chamada à API externa
    → Regista ExternalAiConsultation
    → Captura conhecimento (KnowledgeCapture) se aprovado
    → Retorna resposta
```

### 4.2 Estado real

| Etapa | Estado | Problema |
|-------|--------|----------|
| Endpoint existe | ✅ | ExternalAiEndpointModule |
| Handler existe | ✅ | `QueryExternalAISimple.cs`, `QueryExternalAIAdvanced.cs` |
| Valida política | ⚠️ | Schema existe; enforcement real incerto |
| Seleciona provider | ⚠️ | ExternalAiProvider carregado; seleção incerta |
| Executa chamada | ⚠️ | HTTP client call — configuração incerta |
| Regista consulta | ⚠️ | ExternalAiConsultation persistida — parcial |
| Captura knowledge | ⚠️ | KnowledgeCapture — workflow incompleto |

---

## 5. Etapas sem backend real identificadas

| Etapa | Fluxo | Evidência |
|-------|-------|-----------|
| Tool calling em agents | Agent Execution | AllowedTools nunca despachados (CR-2) |
| Streaming de chat | Chat | Sem SSE/WebSocket implementado |
| Retrieval real | Chat + Orchestration | Serviços de retrieval podem ser stub |
| Routing inteligente | Chat | AIRoutingStrategy schema existe mas execução incerta |
| Rate limiting | Todos | Quotas em schema, enforcement ausente |

---

## 6. Etapas cosméticas identificadas

| Componente | Ficheiro | Problema |
|------------|----------|----------|
| AssistantPanel.tsx | `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx` | Contém gerador de respostas mock |
| IDE Integrations | `IdeIntegrationsPage.tsx` | UI completa sem backend real |
| Model Registry (frontend) | `ModelRegistryPage.tsx` | Botão de registo desativado |
| AI Analysis | `AiAnalysisPage.tsx` | Preview mode — backend parcial |

---

## 7. Correções necessárias para fluxos ficarem reais

| # | Correção | Prioridade | Esforço |
|---|----------|------------|---------|
| E2E-01 | Implementar streaming SSE para chat | P1 | 16h |
| E2E-02 | Implementar framework de tool calling para agents | P1 | 24h |
| E2E-03 | Remover mock response generator do AssistantPanel | P0 | 2h |
| E2E-04 | Validar e corrigir serviços de retrieval | P1 | 16h |
| E2E-05 | Validar routing de modelos ponta a ponta | P2 | 8h |
| E2E-06 | Implementar enforcement de quotas em Runtime | P2 | 8h |
| E2E-07 | Validar 3+ features de Orchestration ponta a ponta | P2 | 16h |
| E2E-08 | Validar External AI query ponta a ponta | P2 | 8h |
| E2E-09 | Verificar persistência de token usage em todos os cenários | P2 | 4h |
| E2E-10 | Corrigir auditoria para cobrir todos os fluxos | P2 | 8h |

---

## 8. Resumo

| Fluxo | Estado | Bloqueador |
|-------|--------|------------|
| Chat de IA | ⚠️ Funcional parcial | Streaming ausente, mock no frontend |
| Execução de Agent | 🔴 Incompleto | Tools NÃO executam |
| Orchestration | ⚠️ Parcial | Maioria dos handlers provavelmente stubs |
| External AI | ⚠️ Parcial | Integração real com APIs externas incerta |
| **Maturidade E2E** | **🔴 ~25%** | — |
