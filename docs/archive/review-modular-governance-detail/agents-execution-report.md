# Auditoria — Execução de Agentes de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação:** EXECUÇÃO_REAL_APARENTE — pipeline completo de 12 passos com inferência real e persistência.

---

## 1. Resumo

A execução de agentes no NexTraceOne é **real e não simulada**. O `AiAgentRuntimeService` implementa um pipeline completo de 12 passos que resolve o agente, valida, selecciona modelo e provider, constrói o prompt, **executa inferência real** contra Ollama ou OpenAI, persiste o resultado e gera artefactos. Cada execução é rastreada com tokens, custo, duração e estado.

---

## 2. Pipeline de execução — 12 passos

```
1. Resolver agente (por ID)
2. Validar agente (PublicationStatus, permissões)
3. Resolver modelo (PreferredModelId ou override do utilizador)
4. Validar acesso ao modelo (IAiModelAuthorizationService)
5. Resolver provider (IAiProviderFactory)
6. Construir prompt de sistema (SystemPrompt do agente)
7. Montar input do utilizador
8. ──── EXECUTAR INFERÊNCIA ──── (chamada HTTP real ao provider)
9. Processar resposta
10. Persistir execução (AiAgentExecution)
11. Gerar artefactos (AiAgentArtifact)
12. Actualizar contadores (ExecutionCount, tokens, custo)
```

### Ficheiro principal

`AiAgentRuntimeService` — serviço completo com orquestração de todo o pipeline.

---

## 3. Entidade AiAgentExecution

Regista **cada execução** de um agente com todos os metadados necessários.

| Campo | Tipo | Propósito |
|---|---|---|
| Id | StronglyTypedId | Identificador único da execução |
| AgentId | ref | Agente executado |
| RequesterId | ref | Utilizador que solicitou |
| Status | enum | Pending / Running / Completed / Failed / Cancelled |
| Input | string (JSON) | Dados de entrada fornecidos |
| Output | string (JSON) | Resultado da inferência |
| ModelIdUsed | ref | Modelo que executou |
| Cost | decimal | Custo estimado da execução |
| Duration | TimeSpan | Duração total |
| StartedAt | DateTimeOffset | Início da execução |
| CompletedAt | DateTimeOffset? | Fim da execução |
| Error | string? | Erro caso tenha falhado |
| TokensUsed | int | Tokens consumidos |

### Ciclo de vida do estado

```
Pending → Running → Completed
                  → Failed (com Error preenchido)
                  → Cancelled
```

---

## 4. Entidade AiAgentArtifact

Artefactos gerados por execuções de agentes (contratos, schemas, análises).

| Campo | Tipo | Propósito |
|---|---|---|
| Id | StronglyTypedId | Identificador |
| AgentId | ref | Agente que gerou |
| ExecutionId | ref | Execução associada |
| Type | string | Tipo de artefacto (OpenAPI, WSDL, Avro, etc.) |
| Content | string | Conteúdo gerado |
| ReviewStatus | enum | PendingReview / Approved / Rejected / Archived |
| ReviewedBy | ref? | Quem revisou |
| ReviewedAt | DateTimeOffset? | Quando foi revisado |

### Human-in-the-loop

Os artefactos **requerem revisão explícita** antes de serem utilizados:

```
PendingReview → Approved (pode ser utilizado)
             → Rejected (descartado)
             → Archived (mantido para histórico)
```

**Nota:** O workflow de aprovação existe ao nível da entidade, mas **não está integrado** com um sistema de aprovação formal (notificações, SLAs, escalonamento).

---

## 5. Tipos de artefactos gerados pelos agentes oficiais

| Agente | Tipo de artefacto | Formato |
|---|---|---|
| API Contract Draft Generator | Contrato API | OpenAPI 3.1 YAML |
| Kafka Schema Contract Designer | Schema de evento | Avro / JSON Schema |
| SOAP Contract Author | Contrato SOAP | WSDL / XML |
| API Test Scenario Generator | Cenários de teste | Texto estruturado |
| Service Health Analyzer | Análise de saúde | Relatório estruturado |
| SLA Compliance Checker | Relatório de SLA | Relatório estruturado |
| Change Impact Evaluator | Análise de impacto | Relatório estruturado |
| Release Risk Evaluator | Avaliação de risco | Relatório estruturado |
| Incident Root Cause Investigator | Análise de causa raiz | Relatório estruturado |
| Security Posture Assessor | Avaliação de segurança | Relatório estruturado |

---

## 6. Rastreamento de execução

### Métricas capturadas por execução

| Métrica | Propósito |
|---|---|
| Duration | Tempo total de execução (inclui latência de rede) |
| TokensUsed | Tokens de input + output |
| Cost | Custo estimado baseado no modelo utilizado |
| Status | Resultado (Completed/Failed/Cancelled) |
| Error | Detalhe do erro (se aplicável) |
| ModelIdUsed | Modelo efectivamente utilizado (pode diferir do preferido) |

### Integração com auditoria

Cada execução de agente também gera:
- `AIUsageEntry` — entrada de auditoria geral
- `AiExternalInferenceRecord` — se o provider for externo
- `AiRoutingDecision` — se houver decisão de routing

---

## 7. Evidências de execução real

| Evidência | Significado |
|---|---|
| HTTP POST real para Ollama/OpenAI | Inferência é executada contra providers reais |
| Parsing de resposta | Resposta JSON é processada e extraída |
| Contagem de tokens | Tokens são contados e registados |
| Duração medida | Tempo real de execução é capturado |
| Resultado persistido | Output é guardado na base de dados |
| Artefactos gerados | Conteúdo real é produzido e armazenado |
| Zero mocks em produção | Nenhum mock no código de produção — stubs apenas para conectores futuros |

---

## 8. Lacunas identificadas

| # | Lacuna | Severidade | Detalhe |
|---|---|---|---|
| 1 | Tools não executados | Alta | `AllowedTools` declarado mas tools não invocados em runtime — agentes apenas geram texto |
| 2 | Sem streaming | Média | Execução é síncrona — utilizador espera resposta completa |
| 3 | Sem execução encadeada | Média | Agentes não podem chamar outros agentes |
| 4 | Workflow de aprovação incompleto | Média | ReviewStatus existe mas sem notificações, SLAs ou escalonamento |
| 5 | Sem retry automático | Baixa | Falhas não são automaticamente re-executadas |
| 6 | Sem timeout configurável por agente | Baixa | Timeout é do provider, não do agente |
| 7 | Sem execução em batch | Baixa | Não é possível executar agente para múltiplos inputs simultaneamente |

---

## 9. Comparação com frameworks de mercado

| Capacidade | NexTraceOne | Semantic Kernel | LangChain |
|---|---|---|---|
| Execução de inferência | ✅ Directa (HTTP) | ✅ | ✅ |
| Tool calling | ❌ Declarado mas não executado | ✅ Nativo | ✅ Nativo |
| Chaining de agentes | ❌ | ✅ | ✅ |
| Memory/RAG | ⚠️ Parcial | ✅ | ✅ |
| Streaming | ❌ | ✅ | ✅ |
| Governança empresarial | ✅ Nativo | ❌ Requer extensão | ❌ Requer extensão |
| Auditoria | ✅ Completa | ❌ Requer extensão | ❌ Requer extensão |
| Políticas de acesso | ✅ Nativas | ❌ | ❌ |

**Nota:** O NexTraceOne diferencia-se pela governança empresarial nativa que frameworks open-source não oferecem.

---

## 10. Recomendações

1. **Implementar execução de tools** — criar `IToolExecutionService` que interpreta `AllowedTools` e executa acções reais (consulta de BD, chamada de API, leitura de ficheiros)
2. **Considerar Semantic Kernel** — adoptar como framework para tool calling e chaining sem reescrever a camada de governança
3. **Implementar streaming** — permitir respostas incrementais para execuções longas
4. **Workflow de aprovação formal** — integrar artefactos com notificações e SLAs
5. **Timeout por agente** — permitir configurar timeout individual por agente
6. **Execução encadeada** — permitir agentes chamar outros agentes para tarefas complexas

---

> **Veredicto:** A execução de agentes é **genuinamente real** com pipeline completo, persistência de resultados e rastreabilidade. A principal evolução necessária é a **execução de tools** para expandir as capacidades dos agentes além da geração de texto.
