# Auditoria — Análise de Gaps entre Camadas (UI ↔ Backend ↔ BD) do Módulo de IA

> **Data:** 2025-07-15
> **Classificação:** GAPS IDENTIFICADOS — 12 desalinhamentos entre camadas documentados com severidade e acção correctiva.

---

## 1. Resumo

Esta análise cruza as três camadas do módulo de IA (Frontend/UI, Backend/API, Base de Dados) para identificar desalinhamentos onde uma camada declara uma capacidade que outra camada não implementa ou implementa de forma diferente. Foram identificados **12 gaps** entre camadas, dos quais 3 são de severidade alta, 5 de severidade média e 4 de severidade baixa.

---

## 2. Matriz de alinhamento entre camadas

### Legenda

- ✅ Implementado e funcional
- ⚠️ Existe mas parcial/incompleto
- ❌ Não implementado
- 🔇 Não aplicável

| Funcionalidade | UI (Frontend) | Backend (API) | BD (Entidades) | Alinhamento |
|---|---|---|---|---|
| Chat principal | ✅ 1 216 linhas | ✅ Inferência real | ✅ Conversas persistidas | ✅ ALINHADO |
| Selecção de modelo | ✅ Selector | ✅ Resolve + valida | ✅ AIModel + AIAccessPolicy | ✅ ALINHADO |
| Agentes — catálogo | ✅ 711 linhas | ✅ CRUD completo | ✅ AiAgent | ✅ ALINHADO |
| Agentes — execução | ✅ Botão executar | ✅ Pipeline 12 passos | ✅ AiAgentExecution | ✅ ALINHADO |
| Auditoria | ✅ 240 linhas | ✅ Registos completos | ✅ AIUsageEntry | ✅ ALINHADO |
| Políticas de acesso | ✅ 185 linhas | ✅ Verificação | ✅ AIAccessPolicy | ✅ ALINHADO |
| Routing | ✅ 462 linhas | ✅ Estratégias | ✅ AIRoutingStrategy | ✅ ALINHADO |
| Token budget | ✅ 196 linhas | ✅ Quotas | ✅ AiTokenQuotaPolicy | ✅ ALINHADO |
| Registo de modelos | ⚠️ **Botão desactivado** | ✅ Suporta | ✅ Entidades completas | ⚠️ GAP UI |
| AssistantPanel | ⚠️ **Mock** | ✅ Chat real funciona | ✅ Persistência | ⚠️ GAP UI |
| Tool execution | ⚠️ UI declara tools | ⚠️ **Não executa** | ✅ AllowedTools armazenado | ❌ GAP CROSS |
| IDE integrations | ✅ 418 linhas | ✅ Registo de clientes | ✅ Entidades | ⚠️ **Extensão real ausente** |
| Streaming | ❌ | ❌ | 🔇 | ❌ NÃO EXISTE |
| Embeddings | ❌ | ❌ | ✅ Flag SupportsEmbeddings | ⚠️ GAP BD→Backend |
| 3 providers inactivos | ✅ Listados na UI | ✅ Configurados | ✅ Semeados | ⚠️ **Requerem API keys** |
| Knowledge capture | ❌ UI não expõe | ⚠️ Parcial | ✅ KnowledgeCaptureEntry | ⚠️ GAP BD→UI |

---

## 3. Análise detalhada de cada gap

### GAP-01: Registo de modelos — UI desactivada

| Camada | Estado |
|---|---|
| UI | `ModelRegistryPage.tsx` — botão "Register Model" **desactivado** (placeholder) |
| Backend | Endpoints de criação de modelo existem e funcionam |
| BD | `AIModel` com 40+ propriedades completas |

**Severidade:** Média
**Impacto:** Administradores não podem adicionar novos modelos pela interface.
**Acção:** Activar botão e implementar formulário de registo.

---

### GAP-02: AssistantPanel — mock no sidebar

| Camada | Estado |
|---|---|
| UI | `AssistantPanel.tsx` — comentário "Mock contextual response generator" |
| Backend | Chat real funciona via `AiAssistantPage.tsx` |
| BD | Conversas e mensagens persistidas normalmente |

**Severidade:** Baixa
**Impacto:** Apenas o painel lateral contextual é mock; o chat principal é real.
**Acção:** Substituir mock por integração com chat real para assistência contextual.
**Nota:** Este gap **não afecta** o chat principal que é funcional.

---

### GAP-03: Tool execution — declarado mas não executado

| Camada | Estado |
|---|---|
| UI | Agentes declaram `AllowedTools` na definição |
| Backend | `AiAgentRuntimeService` **não invoca tools** — campo ignorado em runtime |
| BD | `AllowedTools` armazenado como propriedade de `AiAgent` |

**Severidade:** Alta
**Impacto:** Agentes geram apenas texto; não podem consultar dados reais (telemetria, incidentes, serviços).
**Acção:** Implementar `IToolRegistry`, `IToolExecutor` e integrar com pipeline de execução.

---

### GAP-04: IDE integrations — DB + UI sem extensão real

| Camada | Estado |
|---|---|
| UI | `IdeIntegrationsPage.tsx` (418 linhas) — gestão completa de clientes IDE |
| Backend | Registo de clientes, políticas de capacidade |
| BD | Entidades de registo de clientes IDE |
| Repositório | **Nenhuma extensão real** para VS Code ou Visual Studio |

**Severidade:** Média
**Impacto:** Toda a infra de gestão de IDE existe mas não há extensão para utilizar.
**Acção:** Desenvolver extensão mínima para VS Code como prova de conceito.

---

### GAP-05: Streaming — não implementado em nenhuma camada

| Camada | Estado |
|---|---|
| UI | Request/response, sem streaming |
| Backend | Chamadas HTTP síncronas aos providers |
| BD | Sem suporte a mensagens parciais |
| Modelos | `SupportsStreaming` existe como flag nos modelos |

**Severidade:** Alta
**Impacto:** Experiência de utilizador degradada — utilizador espera resposta completa sem feedback visual.
**Acção:** Implementar Server-Sent Events (SSE) ou WebSocket para streaming.

---

### GAP-06: 3 providers inactivos

| Camada | Estado |
|---|---|
| UI | Providers listados com estado "Inactive" |
| Backend | Configurados e validados — mas não funcionam sem API keys |
| BD | Semeados com configuração completa |

**Severidade:** Média
**Impacto:** Apenas Ollama local disponível; sem acesso a modelos mais poderosos.
**Acção:** Documentar processo de activação e configuração de API keys.

**Providers afectados:**

| Provider | Requisito |
|---|---|
| OpenAI | API key |
| Azure OpenAI | API key + endpoint |
| Google Gemini | API key |

---

### GAP-07: Embeddings — flag sem implementação

| Camada | Estado |
|---|---|
| UI | Sem página de embeddings |
| Backend | Sem endpoint de embeddings |
| BD | `SupportsEmbeddings` flag existe em `AIModel`, `IsDefaultForEmbeddings` |

**Severidade:** Baixa
**Impacto:** RAG não pode utilizar embeddings gerados pelo sistema.
**Acção:** Implementar endpoint e serviço de embeddings quando RAG for priorizado.

---

### GAP-08: Knowledge capture — BD sem UI

| Camada | Estado |
|---|---|
| UI | Sem página dedicada a captura de conhecimento |
| Backend | `KnowledgeCaptureEntry` existe mas workflow não claro |
| BD | Entidade existe com campos completos |

**Severidade:** Média
**Impacto:** Conhecimento operacional não pode ser capturado e reutilizado pela IA.
**Acção:** Implementar UI e workflow de captura de conhecimento.

---

### GAP-09: Retrieval services — possivelmente stubs

| Camada | Estado |
|---|---|
| UI | Toggles de contexto existem (Services, Contracts, etc.) |
| Backend | `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` registados |
| BD | `AIKnowledgeSource` configurável |

**Severidade:** Alta
**Impacto:** Se os serviços de retrieval são stubs, o contexto injectado no prompt é vazio ou genérico.
**Acção:** Validar implementação real dos serviços; completar se forem stubs.

---

### GAP-10: Migrações — dívida técnica

| Camada | Estado |
|---|---|
| BD | AiGovernanceDbContext tem **7 migrações** incluindo fixes de tipo TenantId |

**Severidade:** Baixa
**Impacto:** Dívida técnica de migrações; não afecta funcionalidade.
**Acção:** Consolidar migrações quando oportuno.

---

### GAP-11: Context assembly — parcialmente implementado

| Camada | Estado |
|---|---|
| UI | 5 toggles de contexto enviados como context bundle |
| Backend | `SendAssistantMessage` tem context helper mas montagem pode ser parcial |
| BD | Dados de serviços/contratos/incidentes existem noutros módulos |

**Severidade:** Média
**Impacto:** Agentes podem não receber dados reais dos domínios do produto.
**Acção:** Testar e completar integração de contexto entre módulo de IA e outros módulos.

---

### GAP-12: AiSourceRegistryService — stub para conectores futuros

| Camada | Estado |
|---|---|
| Backend | Health check tem placeholder para futuros tipos de conectores |

**Severidade:** Baixa
**Impacto:** Conectores futuros ainda não implementados; stub é adequado.
**Acção:** Implementar conectores conforme necessário.

---

## 4. Sumário de severidade

| Severidade | Quantidade | Gaps |
|---|---|---|
| Alta | 3 | GAP-03 (tools), GAP-05 (streaming), GAP-09 (retrieval) |
| Média | 5 | GAP-01 (model reg), GAP-04 (IDE), GAP-06 (providers), GAP-08 (knowledge), GAP-11 (context) |
| Baixa | 4 | GAP-02 (AssistantPanel), GAP-07 (embeddings), GAP-10 (migrações), GAP-12 (source registry) |

---

## 5. Priorização de resolução

### Fase 1 — Gaps de alta severidade (impacto funcional directo)

1. **GAP-03:** Implementar execução de tools
2. **GAP-09:** Validar e completar serviços de retrieval
3. **GAP-05:** Implementar streaming de respostas

### Fase 2 — Gaps de média severidade (completude do produto)

4. **GAP-01:** Activar registo de modelos na UI
5. **GAP-06:** Documentar activação de providers
6. **GAP-11:** Completar montagem de contexto
7. **GAP-08:** Implementar UI de knowledge capture
8. **GAP-04:** Desenvolver extensão IDE mínima

### Fase 3 — Gaps de baixa severidade (melhorias incrementais)

9. **GAP-02:** Substituir mock do AssistantPanel
10. **GAP-07:** Implementar endpoint de embeddings
11. **GAP-10:** Consolidar migrações
12. **GAP-12:** Implementar conectores futuros

---

> **Veredicto:** A maioria das funcionalidades está **alinhada entre camadas** (chat, agentes, auditoria, políticas, routing, budgets). Os gaps são **focalizados** em capacidades avançadas (tools, streaming, retrieval) e completude de UI (registo de modelos, knowledge capture). Nenhum gap é arquitectural — todos são resolvíveis com implementação incremental.
