# Auditoria — Rastreabilidade e Observabilidade de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** RASTREÁVEL — trilha de auditoria abrangente adequada para conformidade empresarial.

---

## 1. Resumo

O NexTraceOne implementa uma **trilha de auditoria completa** para todas as operações de IA. Cada chamada de inferência, cada decisão de routing, cada execução de agente e cada acção de governança é registada com metadados ricos. A `AiAuditPage.tsx` oferece uma UI completa para consulta e filtragem. O sistema é adequado para requisitos de conformidade empresarial.

---

## 2. Entidades de auditoria

### 2.1 AIUsageEntry — auditoria geral por chamada

Regista **cada utilização** da IA com metadados completos.

| Campo | Tipo | Propósito |
|---|---|---|
| UserId | ref | Utilizador que fez o pedido |
| ModelId | ref | Modelo solicitado |
| Provider | string | Provider que executou |
| IsInternal | bool | Se o modelo é interno/local |
| Tokens | int | Tokens consumidos (input + output) |
| Result | enum | Allowed / Blocked / QuotaExceeded |
| ClientType | string | Tipo de cliente (Web, IDE, API) |
| PolicyName | string | Política que determinou a decisão |
| ContextScope | string | Scope de contexto utilizado |
| CorrelationId | string | ID para correlação entre sistemas |
| ConversationId | ref? | Conversa associada (se aplicável) |
| CreatedAt | DateTimeOffset | Timestamp da chamada |
| CreatedBy | string | Criador do registo |

### 2.2 AiExternalInferenceRecord — chamadas externas

Regista **cada chamada a providers externos** com dados financeiros e de performance.

| Campo | Tipo | Propósito |
|---|---|---|
| ProviderId | ref | Provider externo chamado |
| ModelId | ref | Modelo utilizado |
| ExternalRequestId | string | ID do pedido no provider externo |
| InputTokens | int | Tokens de entrada |
| OutputTokens | int | Tokens de saída |
| Cost | decimal | Custo da chamada |
| Duration | TimeSpan | Duração da chamada |
| Success | bool | Se a chamada foi bem-sucedida |
| Error | string? | Detalhe do erro (se aplicável) |

### 2.3 AiRoutingDecision — decisões de routing

Regista **cada decisão de routing** tomada pelo sistema.

| Campo | Tipo | Propósito |
|---|---|---|
| SourceContext | string | Contexto de origem do pedido |
| SelectedProvider | string | Provider seleccionado |
| SelectedModel | string | Modelo seleccionado |
| Confidence | decimal | Nível de confiança da decisão |
| ExecutedAt | DateTimeOffset | Timestamp da decisão |
| Cost | decimal | Custo associado |
| TokensUsed | int | Tokens consumidos |

### 2.4 AiAgentExecution — execuções de agentes

| Campo | Tipo | Propósito |
|---|---|---|
| AgentId | ref | Agente executado |
| RequesterId | ref | Utilizador solicitante |
| Status | enum | Pending / Running / Completed / Failed / Cancelled |
| Input | string | Dados de entrada |
| Output | string | Resultado |
| ModelIdUsed | ref | Modelo utilizado |
| Cost | decimal | Custo |
| Duration | TimeSpan | Duração |
| TokensUsed | int | Tokens |
| Error | string? | Erro (se aplicável) |

### 2.5 Metadados de conversa

Cada mensagem (`AiMessage`) rastreia:

| Campo | Propósito |
|---|---|
| ModelName | Nome do modelo que respondeu |
| Provider | Provider que executou |
| IsInternalModel | Modelo interno ou externo |
| Tokens | Tokens consumidos |
| AppliedPolicy | Política aplicada |
| GroundingSources | Fontes de grounding utilizadas |
| ContextReferences | Referências de contexto injectadas |
| CorrelationId | ID de correlação |

---

## 3. Interceptor padrão de auditoria

Todas as entidades do módulo de IA possuem campos de auditoria automática:

| Campo | Tipo | Preenchimento |
|---|---|---|
| CreatedAt | DateTimeOffset | Automático na criação |
| CreatedBy | string | Utilizador autenticado |
| UpdatedAt | DateTimeOffset? | Automático na actualização |
| UpdatedBy | string? | Utilizador que actualizou |
| IsDeleted | bool | Soft delete |
| DeletedAt | DateTimeOffset? | Timestamp do soft delete |

---

## 4. UI de auditoria — AiAuditPage.tsx

| Funcionalidade | Estado |
|---|---|
| Ficheiro | `src/frontend/…/AiAuditPage.tsx` (240 linhas) |
| Listagem de entradas | ✅ |
| Pesquisa | ✅ |
| Filtro por resultado (Allowed/Blocked/QuotaExceeded) | ✅ |
| Exibição de metadados | ✅ Todos os campos visíveis |
| Paginação | ✅ |
| Exportação | ⚠️ Não confirmada |

---

## 5. Cobertura de rastreabilidade

### 5.1 Matriz de eventos auditados

| Evento | Entidade | Estado |
|---|---|---|
| Pedido de chat | AIUsageEntry | ✅ |
| Resposta de chat | AiMessage (metadados) | ✅ |
| Pedido bloqueado | AIUsageEntry (result=Blocked) | ✅ |
| Quota excedida | AIUsageEntry (result=QuotaExceeded) | ✅ |
| Chamada externa | AiExternalInferenceRecord | ✅ |
| Decisão de routing | AiRoutingDecision | ✅ |
| Execução de agente | AiAgentExecution | ✅ |
| Geração de artefacto | AiAgentArtifact | ✅ |
| Revisão de artefacto | AiAgentArtifact (ReviewedBy/At) | ✅ |
| Criação de conversa | AiAssistantConversation (CreatedAt/By) | ✅ |
| Criação de agente | AiAgent (CreatedAt/By) | ✅ |
| Alteração de política | AIAccessPolicy (UpdatedAt/By) | ✅ |
| Alteração de modelo | AIModel (UpdatedAt/By) | ✅ |

### 5.2 Correlação entre entidades

```
AIUsageEntry.CorrelationId
  ↔ AiMessage.CorrelationId
  ↔ AiExternalInferenceRecord (por timestamp/provider)
  ↔ AiRoutingDecision (por timestamp/context)
  ↔ AiAgentExecution (por AgentId + RequesterId + timestamp)
```

---

## 6. Métricas de FinOps capturadas

| Métrica | Fonte | Propósito |
|---|---|---|
| Tokens por chamada | AIUsageEntry, AiMessage | Consumo de tokens |
| Custo por chamada externa | AiExternalInferenceRecord.Cost | Custo financeiro |
| Custo por execução de agente | AiAgentExecution.Cost | Custo por agente |
| Duração por chamada | AiExternalInferenceRecord.Duration | Performance |
| Duração por agente | AiAgentExecution.Duration | Performance de agentes |
| Tokens por utilizador/equipa | AiTokenUsageLedger | Consumo agregado |
| Modelos internos vs externos | AIUsageEntry.IsInternal | Distribuição de uso |

---

## 7. Conformidade empresarial

| Requisito | Estado | Mecanismo |
|---|---|---|
| Quem usou IA | ✅ | UserId em todas as entradas |
| Que modelo foi usado | ✅ | ModelId/ModelName rastreado |
| Interno ou externo | ✅ | IsInternal flag |
| Que dados saíram | ⚠️ Parcial | Input/Output registado para agentes; conteúdo de chat não claramente registado como dados de saída |
| Que política foi aplicada | ✅ | PolicyName em AIUsageEntry |
| Resultado da autorização | ✅ | Result (Allowed/Blocked/QuotaExceeded) |
| Custo | ✅ | Cost em AiExternalInferenceRecord e AiAgentExecution |
| Correlação | ✅ | CorrelationId transversal |
| Soft delete | ✅ | IsDeleted em todas as entidades |
| Imutabilidade de logs | ⚠️ | Entradas de auditoria na mesma BD — sem write-once storage |

---

## 8. Lacunas identificadas

| # | Lacuna | Severidade | Detalhe |
|---|---|---|---|
| 1 | Sem exportação de auditoria | Média | Não confirmada exportação para CSV/PDF na UI |
| 2 | Logs na mesma BD | Média | Entradas de auditoria não estão em storage imutável/write-once |
| 3 | Conteúdo de chat não classificado | Média | Input/output de chat não marcado como dados sensíveis |
| 4 | Sem retenção configurável | Média | Não há política de retenção/purge de dados de auditoria |
| 5 | Sem alertas de anomalia | Baixa | Não há detecção automática de padrões anómalos de uso |
| 6 | Sem dashboards executivos | Baixa | Auditoria é tabular, sem visualização gráfica de tendências |

---

## 9. Recomendações

### Prioridade Alta

1. **Exportação de auditoria** — implementar exportação para CSV/JSON para conformidade
2. **Storage imutável** — considerar append-only table ou external log store para imutabilidade
3. **Política de retenção** — definir e implementar retenção configurável de dados de auditoria

### Prioridade Média

4. **Classificação de dados sensíveis** — marcar conteúdo de chat que possa conter dados sensíveis
5. **Dashboards executivos** — gráficos de uso, custo e tendências para persona Executive
6. **Alertas de anomalia** — detecção de padrões incomuns (uso excessivo, tentativas bloqueadas)

### Prioridade Baixa

7. **Integração com SIEM** — exportar eventos de auditoria para sistemas externos de segurança
8. **Relatórios periódicos** — geração automática de relatórios de uso por período

---

> **Veredicto:** A rastreabilidade de IA é **abrangente e adequada para conformidade empresarial**. Cada chamada, cada decisão e cada execução é registada com metadados ricos. As melhorias são incrementais: exportação, imutabilidade e dashboards. A base arquitectural é sólida.
