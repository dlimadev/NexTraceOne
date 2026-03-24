# Auditoria — Chat de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** FUNCIONAL_APARENTE — inferência real executada, conversas persistidas, metadados rastreados.

---

## 1. Resumo

O chat de IA do NexTraceOne executa **inferência real** através de dois providers (Ollama local e OpenAI cloud). A UI principal (`AiAssistantPage.tsx`) é uma implementação completa com 1 216 linhas, suportando lista de conversas, thread de mensagens, selecção de modelo, painel de agentes e toggles de contexto.

| Métrica | Valor |
|---|---|
| Linhas de UI (página principal) | 1 216 |
| Funções API no frontend | 44 |
| Chaves i18n | 100+ |
| Providers reais | 2 (OpenAI + Ollama) |
| Entidades de persistência | AiAssistantConversation + AiMessage |

---

## 2. Fluxo de execução do chat

### 2.1 Frontend → Backend

```
AiAssistantPage.tsx
  → sendMessage(conversationId, content, modelId, contextBundle)
    → POST /api/ai/assistant/conversations/{id}/messages
      → SendAssistantMessage handler
        → resolve model + provider
        → build prompt (system + context + history)
        → ExecuteAiChat
          → OpenAiHttpClient.SendAsync() ou OllamaHttpClient.SendAsync()
        → persist message + metadata
        → return response with metadata
```

### 2.2 Chamadas API utilizadas pelo frontend

| Função | Método | Endpoint |
|---|---|---|
| `listConversations` | GET | `/api/ai/assistant/conversations` |
| `getConversation` | GET | `/api/ai/assistant/conversations/{id}` |
| `createConversation` | POST | `/api/ai/assistant/conversations` |
| `sendMessage` | POST | `/api/ai/assistant/conversations/{id}/messages` |
| `checkProvidersHealth` | GET | `/api/ai/providers/health` |
| `listAvailableModels` | GET | `/api/ai/models/available` |
| `listAgents` | GET | `/api/ai/agents` |

---

## 3. Providers de execução

### 3.1 OpenAiHttpClient

| Aspecto | Detalhe |
|---|---|
| Ficheiro | `src/modules/aiknowledge/…/OpenAiHttpClient.cs` |
| Endpoint | HTTP POST para `/v1/chat/completions` |
| Autenticação | Bearer token (API key) |
| Parsing | Parsing real da resposta JSON |
| Estado | Funcional — requer API key configurada |

### 3.2 OllamaHttpClient

| Aspecto | Detalhe |
|---|---|
| Ficheiro | `src/modules/aiknowledge/…/OllamaHttpClient.cs` |
| Endpoint | POST para `/api/chat` |
| Retry | Lógica de retry implementada |
| Enumeração de modelos | GET `/api/tags` |
| Configuração padrão | `http://localhost:11434`, modelo `deepseek-r1:1.5b` |
| Estado | Funcional — activo por defeito |

### 3.3 Handler de execução (ExecuteAiChat)

- Executa inferência real contra o provider seleccionado
- Guarda contagem de tokens na base de dados
- Regista entrada de auditoria (`AIUsageEntry`)
- Mede duração da chamada
- Retorna resposta com metadados completos

---

## 4. Persistência de conversas

### Entidades

| Entidade | DbContext | Campos-chave |
|---|---|---|
| `AiAssistantConversation` | AiGovernanceDbContext | Id, UserId, Title, CreatedAt, LastMessageAt |
| `AiMessage` | AiGovernanceDbContext | Id, ConversationId, Role, Content, ModelName, Provider, IsInternalModel, Tokens, AppliedPolicy, GroundingSources, ContextReferences, CorrelationId |

### Metadados rastreados por mensagem

- `modelName` — nome do modelo utilizado
- `provider` — provider que executou a inferência
- `isInternalModel` — se o modelo é interno (local) ou externo (cloud)
- `tokens` — contagem de tokens (input + output)
- `appliedPolicy` — política de acesso aplicada
- `groundingSources` — fontes de grounding utilizadas
- `contextReferences` — referências de contexto injectadas
- `correlationId` — ID de correlação para rastreabilidade

---

## 5. Selecção de modelo

| Aspecto | Estado |
|---|---|
| UI de selecção | ✅ Selector na página do chat |
| Agrupamento | ✅ Modelos agrupados por interno/externo |
| Restrições de política | ✅ `AIAccessPolicy` controla modelos permitidos/bloqueados |
| Modelos activos por defeito | 1 (DeepSeek R1 1.5B via Ollama) |
| Modelos inactivos | 2 (GPT-4o-mini, GPT-4o — requerem API key) |

---

## 6. Contexto

| Aspecto | Estado | Evidência |
|---|---|---|
| Toggles de contexto | ✅ Existem | Services, Contracts, Incidents, Changes, Runbooks |
| Parsing de context bundle | ✅ Implementado | JSON parsing em `SendAssistantMessage` |
| Injecção real de dados | ⚠️ Parcial | Context helper existe mas grounding real pode ser parcial |

---

## 7. Lacunas identificadas

| # | Lacuna | Severidade | Detalhe |
|---|---|---|---|
| 1 | Sem streaming | Média | Chat é request/response; sem respostas incrementais |
| 2 | AssistantPanel mock | Baixa | Painel lateral contextual (`AssistantPanel.tsx`) tem "Mock contextual response generator" — **não é o chat principal** |
| 3 | Montagem de contexto parcial | Média | Toggles existem mas a injecção real de dados de serviços/contratos/incidentes pode ser incompleta |
| 4 | Texto i18n enganador | Informativo | `"assistantEmptyTitle": "AI Assistant coming soon"` é texto do estado vazio, não reflecte funcionalidade |
| 5 | Sem histórico de edição | Baixa | Mensagens não suportam edição/regeneração |

---

## 8. Evidências de ficheiros

| Ficheiro | Papel |
|---|---|
| `src/frontend/…/AiAssistantPage.tsx` | UI principal do chat (1 216 linhas) |
| `src/frontend/…/AssistantPanel.tsx` | Painel lateral contextual (mock) |
| `src/modules/aiknowledge/…/OpenAiHttpClient.cs` | Cliente HTTP para OpenAI |
| `src/modules/aiknowledge/…/OllamaHttpClient.cs` | Cliente HTTP para Ollama |
| `src/modules/aiknowledge/…/ExecuteAiChat.cs` | Handler de execução de chat |
| `src/modules/aiknowledge/…/SendAssistantMessage.cs` | Handler de envio de mensagem com contexto |
| `src/modules/aiknowledge/…/AiGovernanceDbContext.cs` | DbContext com 19 DbSets |

---

## 9. Recomendações

1. **Implementar streaming** — adoptar Server-Sent Events ou WebSocket para respostas incrementais
2. **Substituir mock do AssistantPanel** — integrar com chat real para assistência contextual no sidebar
3. **Validar grounding de contexto** — testar se toggles de Services/Contracts/Incidents/Changes/Runbooks realmente injectam dados relevantes no prompt
4. **Actualizar texto i18n** — substituir "AI Assistant coming soon" por texto adequado ao estado funcional
5. **Considerar regeneração de mensagens** — permitir re-executar uma mensagem com modelo diferente

---

> **Veredicto:** O chat de IA é **genuinamente funcional** com execução real de inferência, persistência completa e rastreabilidade. As lacunas são de experiência de utilizador (streaming, contexto) e não de arquitectura fundamental.
