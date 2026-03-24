# Auditoria — Prompts, Contexto, Memória e Tools de IA do NexTraceOne

> **Data:** 2025-07-15
> **Classificação por dimensão:**
> - Prompts: ESTRUTURADOS
> - Contexto: PARCIAL
> - Memória: FUNCIONAL
> - Tools: COSMÉTICO_APENAS

---

## 1. Resumo

| Dimensão | Estado | Descrição |
|---|---|---|
| Prompts | ✅ Estruturados | Agentes oficiais com prompts detalhados de 800+ chars, regras específicas e formato de saída |
| Contexto | ⚠️ Parcial | Toggles de contexto existem na UI, parsing de context bundle implementado, grounding real possivelmente incompleto |
| Memória | ✅ Funcional | Conversas + mensagens totalmente persistidas com histórico completo |
| Tools | ❌ Cosmético | Campo AllowedTools declarado em agentes mas nenhum framework de execução implementado |

---

## 2. Prompts — análise detalhada

### 2.1 Estrutura dos prompts de sistema

Os 10 agentes oficiais possuem **prompts de sistema detalhados** (800+ caracteres cada) que seguem uma estrutura consistente:

| Elemento do prompt | Presença | Propósito |
|---|---|---|
| Identidade do agente | ✅ | Define quem é o agente e o seu papel |
| Domínio de actuação | ✅ | Delimita o escopo de análise |
| Regras de comportamento | ✅ | Restrições e directrizes obrigatórias |
| Formato de saída | ✅ | Estrutura esperada da resposta |
| Contexto do NexTraceOne | ✅ | Referências ao produto e aos seus conceitos |
| Exemplos | ⚠️ Variável | Alguns agentes incluem exemplos, outros não |

### 2.2 Configuração por agente

| Propriedade | Limite | Propósito |
|---|---|---|
| SystemPrompt | Até 10 000 caracteres | Prompt de sistema principal |
| InputSchema | JSON Schema | Valida entrada do utilizador |
| OutputSchema | JSON Schema | Define formato esperado da saída |

### 2.3 Categorias de prompts por agente

| Agente | Tipo de prompt | Saída esperada |
|---|---|---|
| API Contract Draft Generator | Geração de artefacto | OpenAPI 3.1 YAML |
| Kafka Schema Contract Designer | Geração de artefacto | Avro / JSON Schema |
| SOAP Contract Author | Geração de artefacto | WSDL |
| API Test Scenario Generator | Geração de conteúdo | Cenários de teste estruturados |
| Service Health Analyzer | Análise | Relatório de saúde |
| Change Impact Evaluator | Análise | Avaliação de impacto |
| Release Risk Evaluator | Análise | Avaliação de risco |
| SLA Compliance Checker | Verificação | Relatório de conformidade |
| Incident Root Cause Investigator | Investigação | Análise de causa raiz |
| Security Posture Assessor | Auditoria | Avaliação de segurança |

---

## 3. Contexto — análise detalhada

### 3.1 Toggles de contexto na UI do chat

A `AiAssistantPage.tsx` oferece **5 toggles de contexto** que o utilizador pode activar:

| Toggle | Domínio | Estado |
|---|---|---|
| Services | Catálogo de serviços | ⚠️ Existe na UI |
| Contracts | Contratos de APIs/eventos | ⚠️ Existe na UI |
| Incidents | Incidentes activos | ⚠️ Existe na UI |
| Changes | Mudanças em produção | ⚠️ Existe na UI |
| Runbooks | Runbooks operacionais | ⚠️ Existe na UI |

### 3.2 Montagem de contexto no backend

| Componente | Ficheiro | Estado |
|---|---|---|
| Context bundle parsing | `SendAssistantMessage` handler | ✅ JSON parsing implementado |
| Context helper | Dentro do handler | ⚠️ Funcionalidade pode ser parcial |

### 3.3 Serviços de retrieval (RAG)

| Serviço | Tipo | Estado |
|---|---|---|
| `DocumentRetrievalService` | Retrieval de documentos | ⚠️ Registado, possivelmente stub |
| `DatabaseRetrievalService` | Retrieval de base de dados | ⚠️ Registado, possivelmente stub |
| `TelemetryRetrievalService` | Retrieval de telemetria | ⚠️ Registado, possivelmente stub |

### 3.4 Fontes de conhecimento

| Entidade | Campos | Propósito |
|---|---|---|
| `AIKnowledgeSource` | Name, Type, Description, Url, Query, Weight, Priority, LastSyncAt | Define fontes de conhecimento para RAG |

### 3.5 Avaliação de contexto

O contexto está **arquitecturalmente preparado** mas a **implementação real de grounding pode ser incompleta**:

- ✅ Toggles existem na UI
- ✅ Context bundle é enviado ao backend
- ✅ Parsing JSON implementado
- ⚠️ A injecção real de dados dos serviços/contratos/incidentes no prompt não está confirmada como funcional
- ⚠️ Serviços de retrieval podem ser stubs/parciais
- ⚠️ `AiSourceRegistryService` tem stub para health check de futuros conectores

---

## 4. Memória — análise detalhada

### 4.1 Persistência de conversas

| Aspecto | Estado | Detalhe |
|---|---|---|
| Conversas | ✅ Persistidas | `AiAssistantConversation` na BD |
| Mensagens | ✅ Persistidas | `AiMessage` com histórico completo |
| Thread history | ✅ Disponível | Histórico do thread enviado ao modelo |
| Metadados | ✅ Completos | Modelo, provider, tokens, política, correlation ID |

### 4.2 Captura de conhecimento

| Entidade | Estado | Propósito |
|---|---|---|
| `KnowledgeCaptureEntry` | ⚠️ Existe | Entidade para captura de conhecimento, workflow pouco claro |

### 4.3 Limites de memória

| Aspecto | Estado |
|---|---|
| Window de contexto | ⚠️ Não há gestão explícita de window (truncamento, sumarização) |
| Memória de longo prazo | ⚠️ Conversas persistidas mas sem sumarização automática |
| Cross-conversation memory | ❌ Não implementada |

---

## 5. Tools — análise detalhada

### 5.1 Estado actual

| Aspecto | Estado | Detalhe |
|---|---|---|
| Campo AllowedTools | ✅ Existe | Propriedade na entidade `AiAgent` |
| Definição de tools | ✅ Declarados | Agentes declaram ferramentas permitidas |
| Framework de execução | ❌ Não existe | Nenhum código que interprete e execute tools |
| Tool calling do modelo | ❌ Não ligado | Flag `SupportsToolCalling` existe no modelo mas não é utilizada |
| Permissões por tool | ❌ Não existe | Sem controlo de acesso granular por ferramenta |

### 5.2 O que falta para tools funcionais

```
1. IToolRegistry — registo de tools disponíveis
2. IToolExecutor — execução de tools individuais
3. IToolPermissionService — verificação de permissões por tool
4. Integração com modelo — enviar definição de tools ao provider
5. Parsing de tool calls — interpretar resposta do modelo que solicita tool
6. Execução em loop — tool call → execute → return result → continue
7. Sandboxing — execução segura de tools
8. Auditoria — registar cada invocação de tool
```

### 5.3 Impacto da ausência de tools

| Cenário | Com tools | Sem tools (actual) |
|---|---|---|
| "Analisa saúde do serviço X" | Agente consulta BD e telemetria | Agente gera texto genérico |
| "Gera contrato para API Y" | Agente consulta schema existente | Agente gera contrato sem contexto real |
| "Investiga incidente Z" | Agente consulta logs e métricas | Agente gera análise hipotética |

---

## 6. Lacunas consolidadas

| # | Dimensão | Lacuna | Severidade |
|---|---|---|---|
| 1 | Tools | Execução não implementada | Alta |
| 2 | Contexto | Grounding real possivelmente incompleto | Alta |
| 3 | Contexto | Serviços de retrieval possivelmente stubs | Média |
| 4 | Memória | Sem gestão de window de contexto | Média |
| 5 | Memória | Sem sumarização automática de conversas longas | Média |
| 6 | Tools | Sem permissões por ferramenta | Média |
| 7 | Memória | Sem memória cross-conversation | Baixa |
| 8 | Prompts | Variabilidade na inclusão de exemplos | Baixa |

---

## 7. Recomendações

### Tools (Prioridade Crítica)

1. **Criar IToolRegistry** e registar tools reais: `query_services`, `query_contracts`, `query_incidents`, `query_telemetry`, `query_changes`
2. **Implementar IToolExecutor** com sandboxing e timeout
3. **Integrar com providers** — enviar definição de tools no pedido ao Ollama/OpenAI
4. **Implementar loop de tool calling** — modelo solicita tool → executor executa → resultado retornado ao modelo

### Contexto (Prioridade Alta)

5. **Validar grounding real** — testar se os toggles de contexto efectivamente injectam dados relevantes
6. **Completar serviços de retrieval** — garantir que DocumentRetrievalService, DatabaseRetrievalService, TelemetryRetrievalService são funcionais

### Memória (Prioridade Média)

7. **Implementar gestão de window** — truncar ou sumarizar histórico quando exceder contexto do modelo
8. **Sumarização automática** — sumarizar conversas longas para preservar contexto relevante

---

> **Veredicto:** Os prompts são **bem estruturados** e a memória é **funcional**. As lacunas críticas estão no **contexto** (grounding possivelmente parcial) e nos **tools** (declarados mas não executados). A implementação de tools é a evolução mais impactante para a capacidade real dos agentes.
