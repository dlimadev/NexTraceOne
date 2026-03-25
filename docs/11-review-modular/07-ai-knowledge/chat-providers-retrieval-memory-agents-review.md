# PARTE 10 — Chat, Providers, Retrieval, Memory & Agents Review

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Chat — estado atual

### 1.1 Componentes

| Componente | Ficheiro | Estado |
|------------|----------|--------|
| Frontend: Chat UI | `AiAssistantPage.tsx` (~57K) | ✅ UI completa |
| Frontend: AssistantPanel | `AssistantPanel.tsx` | 🔴 Contém mock response generator |
| Backend: Send message | `SendAssistantMessage.cs` | ⚠️ Funcional sem streaming |
| Backend: Runtime chat | `ExecuteAiChat.cs` | ⚠️ Funcional sem streaming |
| Persistência: Conversa | `AiAssistantConversation` + `AiMessage` | ✅ Persistido em PostgreSQL |
| API client | `aiGovernance.ts` → `sendMessage()` | ✅ Implementado |

### 1.2 Fluxo real

```
sendMessage() → POST /api/v1/ai/assistant/chat
  → SendAssistantMessage handler
    → Carrega conversa existente (ou cria)
    → Resolve modelo (via política/routing ou padrão)
    → Executa inferência síncrona (HTTP call ao provider)
    → Persiste AiMessage (user + assistant)
    → Retorna conteúdo da resposta
```

### 1.3 Problemas

| # | Problema | Severidade |
|---|----------|------------|
| CH-01 | Streaming não implementado | 🔴 ALTA — UX degradada |
| CH-02 | AssistantPanel.tsx tem mock response generator | 🔴 ALTA — mascara falhas |
| CH-03 | Context assembly/retrieval incerto | 🟠 ALTA — prompt pode ser fixo |
| CH-04 | Token counting pode ser impreciso | 🟡 MÉDIA |
| CH-05 | Sem fallback se provider falhar | 🟠 ALTA |
| CH-06 | Sem cancellation de request longo | 🟡 MÉDIA |

---

## 2. Providers & Models — estado atual

### 2.1 Entidades

| Entidade | Campos chave | Estado |
|----------|-------------|--------|
| `AIModel` | Name, ModelType, Provider, Status, SupportsStreaming, SupportsEmbeddings | ✅ Schema rico |
| `AiProvider` | Name, BaseUrl, ApiKey, HealthStatus, IsActive, MaxConcurrency | ⚠️ Schema OK |

### 2.2 Funcionalidades

| Funcionalidade | Estado | Problema |
|---------------|--------|----------|
| Listar modelos | ✅ Funcional | — |
| Registar modelo | ⚠️ Handler existe | Frontend desativou o botão |
| Atualizar modelo | ⚠️ Funcional | — |
| Listar providers | ✅ Funcional | — |
| Health check de providers | ⚠️ Parcial | Pode não testar conectividade real |
| Ativação/desativação | ⚠️ Schema existe | Workflow incerto |
| Routing inteligente | ⚠️ Schema existe | Estratégia provavelmente não executa |

### 2.3 Problemas

| # | Problema | Severidade |
|---|----------|------------|
| PR-01 | Frontend desativou registo de modelo — inconsistência | 🟠 MÉDIA |
| PR-02 | Health check pode ser superficial | 🟠 MÉDIA |
| PR-03 | ApiKey armazenada — verificar encriptação | 🔴 ALTA (segurança) |
| PR-04 | Routing strategy não executa realmente | 🟡 MÉDIA |
| PR-05 | Sem suporte multi-provider real (fallback chain) | 🟠 MÉDIA |

---

## 3. Retrieval & Context — estado atual

### 3.1 Componentes

| Componente | Estado |
|------------|--------|
| `AIKnowledgeSource` / `AiSource` | ⚠️ Schema existe — duplicação detetada |
| `EnrichContext` feature | ⚠️ Handler existe — grounding real incerto |
| `AIEnrichmentResult` | ⚠️ Schema existe — persistência de resultado |
| Serviços de retrieval no Infrastructure | ❌ Possivelmente stub |

### 3.2 Problemas

| # | Problema | Severidade |
|---|----------|------------|
| RT-01 | Não confirmado se retrieval real existe (embedding search, vector, etc.) | 🔴 ALTA |
| RT-02 | Duas entidades para sources (AiSource + AIKnowledgeSource) | 🟠 MÉDIA |
| RT-03 | Sem evidência de vector store ou embedding pipeline | 🔴 ALTA |
| RT-04 | Context assembly pode ser apenas system prompt fixo | 🔴 ALTA |
| RT-05 | Sem métricas de qualidade de retrieval | 🟡 BAIXA |

### 3.3 Mínimo funcional necessário

1. Pelo menos 1 fonte de conhecimento real (e.g., dados de catálogo)
2. Montagem de contexto com dados reais (não template fixo)
3. Rastreabilidade de sources utilizadas na resposta (ContextSourceIds)
4. Configuração de pesos/relevância por source

---

## 4. Memory & History — estado atual

### 4.1 Componentes

| Componente | Estado |
|------------|--------|
| `AiAssistantConversation` | ✅ Persistida com título, persona, scope |
| `AiMessage` | ✅ Persistida com role, content, tokens |
| `ListConversations` / `GetConversation` | ✅ Funcional |
| `ListMessages` | ✅ Funcional |
| Contexto de conversa (window) | ⚠️ Incerto — pode enviar todo o histórico ou não |

### 4.2 Problemas

| # | Problema | Severidade |
|---|----------|------------|
| ME-01 | Não claro se context window é implementada (sliding window, summarization) | 🟠 MÉDIA |
| ME-02 | Sem limite de mensagens por conversa | 🟡 BAIXA |
| ME-03 | Sem exportação de conversa | 🟡 BAIXA |
| ME-04 | ArchivedAt existe mas archival flow não validado | 🟡 BAIXA |

### 4.3 Avaliação

Memory/History é **o subdomínio mais funcional** do módulo. A persistência é real, a consulta funciona, e o modelo de dados é adequado.

---

## 5. Agents — estado atual

### 5.1 Entidades

| Entidade | Campos chave | Estado |
|----------|-------------|--------|
| `AiAgent` | Name, Category, SystemPrompt, AllowedTools, OwnershipType, PublicationStatus, Visibility | ✅ Schema rico |
| `AiAgentExecution` | AgentId, Status, InputQuery, Output, TokensUsed, ExecutedAt | ⚠️ Parcial |
| `AiAgentArtifact` | AgentId, ArtifactType, Content, ReviewStatus | ⚠️ Parcial |
| `AIExecutionPlan` | PlanSteps, EstimatedTokens, ExecutionStrategy | ⚠️ Schema existe |

### 5.2 Funcionalidades

| Funcionalidade | Estado | Problema |
|---------------|--------|----------|
| Listar agents | ✅ Funcional | — |
| Criar agent | ✅ Funcional | — |
| Atualizar agent | ✅ Funcional | — |
| Executar agent | 🔴 INCOMPLETO | **AllowedTools declarados mas NUNCA despachados** |
| Review de artefactos | ⚠️ Schema existe | Workflow incerto |
| Execution plan | ⚠️ Schema existe | Execução real incerta |

### 5.3 Tool Calling — PROBLEMA CRÍTICO (CR-2)

```
AiAgent.AllowedTools = ["analyze_service", "classify_change", "investigate_incident"]
```

**Estado:** Os tools são **declarados como strings** na definição do agent mas:
- ❌ Não existe `IToolDispatcher` ou interface de despacho
- ❌ Não existe registo de tools disponíveis
- ❌ Não existe validação de permissões por tool
- ❌ Não existe sandbox de execução
- ❌ O handler `ExecuteAgent` provavelmente ignora AllowedTools

**Resultado:** A execução de um agent é essencialmente uma chamada LLM com system prompt, sem capacidade real de usar tools.

### 5.4 Mínimo funcional necessário para agents

1. **IToolRegistry** — registo de tools disponíveis com metadata
2. **IToolDispatcher** — mecanismo de despacho de tool calls
3. **IToolPermissionValidator** — validação de permissões por tool
4. **Tool execution loop** — LLM → tool call → resultado → LLM → resposta final
5. **Timeout e cancellation** — proteção contra loops infinitos
6. **Persistência de tool calls** — rastreabilidade em AiAgentExecution

---

## 6. Resumo geral

| Subdomínio | Maturidade | Problema principal |
|------------|------------|-------------------|
| Chat | 🟠 50% | Sem streaming, mock no frontend |
| Providers & Models | 🟠 45% | Health check e routing incertos |
| Retrieval & Context | 🔴 15% | Sem evidência de retrieval real |
| Memory & History | 🟢 75% | Contexto window incerto |
| Agents | 🔴 25% | Tools NÃO executam (CR-2) |
| **Média** | **🔴 ~35%** | — |
