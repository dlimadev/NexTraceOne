# Inventário de Stubs — NexTraceOne

> Prompt N16 — Parte 3 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este relatório inventaria todos os stubs e implementações parciais relevantes encontrados no backend e frontend do NexTraceOne.

**Total de stubs relevantes encontrados: 11**
- 🔴 MUST_IMPLEMENT: 5
- 🟠 MUST_REMOVE: 0
- 🟡 CAN_DELAY: 4
- ⚪ OUT_OF_SCOPE: 2

---

## 2. Stubs no Backend

### STUB-B01 — AiSourceRegistryService Health Check

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiSourceRegistryService.cs` |
| **Descrição** | Health check é stub — retorna estado fixo sem verificar conectores reais |
| **Comentário** | "Health check é stub — será implementado quando houver conectores reais por tipo de fonte" |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🟡 **CAN_DELAY** — depende de conectores reais ainda inexistentes |

### STUB-B02 — ListSuggestedPrompts (Code-Driven)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/ListSuggestedPrompts/ListSuggestedPrompts.cs` |
| **Descrição** | Prompts sugeridos são definidos em memória (code-driven) em vez de serem configuráveis via DB |
| **Comentário** | "Nota: prompts sugeridos são definidos em memória (code-driven) nesta fase" |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🟡 **CAN_DELAY** — aceitável na fase inicial, migrar para DB quando AI amadurecer |

### STUB-B03 — DatabaseRetrievalService (Proof of Concept)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DatabaseRetrievalService.cs` |
| **Descrição** | Serviço de retrieval que pesquisa apenas AIModels por keyword — descrito como proof of concept |
| **Comentário** | "Demonstra padrão de acesso controlado... Usa IAiModelRepository como proof of concept" |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🔴 **MUST_IMPLEMENT** — retrieval é funcionalidade core do AI module |

### STUB-B04 — AI Tool Execution (Never Executes)

| Campo | Valor |
|---|---|
| **Ficheiro** | Múltiplos handlers em `src/modules/aiknowledge/` |
| **Descrição** | Tools são registados mas nunca executam de facto (CR-2 no remediation plan) |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🔴 **MUST_IMPLEMENT** — funcionalidade core do assistente AI |

### STUB-B05 — AI Streaming (Not Implemented)

| Campo | Valor |
|---|---|
| **Ficheiro** | Módulo AI & Knowledge — runtime layer |
| **Descrição** | Streaming de respostas AI não está implementado; respostas são entregues em bloco |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🔴 **MUST_IMPLEMENT** — experiência de utilizador do assistente depende disto |

### STUB-B06 — CatalogGraphModuleService (Empty Returns)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Services/CatalogGraphModuleService.cs` |
| **Descrição** | Múltiplos métodos retornam `Array.Empty<T>()` — TeamServiceInfo, TeamContractInfo, CrossTeamDependencyInfo |
| **Módulo** | Catalog |
| **Classificação** | 🔴 **MUST_IMPLEMENT** — graph de dependências é funcionalidade core do Catalog |

### STUB-B07 — GenerateDraftFromAi Fallback Estático

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/GenerateDraftFromAi/GenerateDraftFromAi.cs` |
| **Descrição** | Gera template estático como fallback quando IA não está disponível |
| **Comentário** | "Gera template estático como fallback quando IA não está disponível" |
| **Módulo** | Catalog / Contracts |
| **Classificação** | 🟡 **CAN_DELAY** — fallback é padrão defensivo aceitável; real implementation depende do AI module |

### STUB-B08 — ExternalAiRoutingPortAdapter Fallbacks

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/ExternalAiRoutingPortAdapter.cs` |
| **Descrição** | Múltiplas estratégias de fallback (secondary, tertiary) com prefixo `[FALLBACK_PROVIDER_UNAVAILABLE]` |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🟡 **CAN_DELAY** — fallbacks são padrão defensivo, não stub |

---

## 3. Stubs no Frontend

### STUB-F01 — AI Assistant "Coming Soon"

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` |
| **Descrição** | `"assistantEmptyTitle": "AI Assistant coming soon"` — estado vazio do assistente mostra "coming soon" |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🔴 **MUST_IMPLEMENT** — assistente deve ter funcionalidade real ou ser ocultado |

### STUB-F02 — Widget "Coming Soon" (Governance)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` |
| **Descrição** | `"widgetComingSoon": "Data will be available when connected to backend services"` |
| **Módulo** | Governance / Shared |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — mensagem genérica para widgets sem dados; comportamento correto |

### STUB-F03 — AI Grounding Development Message

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` |
| **Descrição** | `"groundingDevelopment": "Context grounding is under active development — full contextual responses...will be available soon"` |
| **Módulo** | AI & Knowledge |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — mensagem informativa honesta sobre estado de desenvolvimento |

---

## 4. Stubs Não Encontrados

- ❌ **Nenhum `throw new NotImplementedException`** encontrado em todo o `src/modules/`
- ❌ **Nenhum TODO** encontrado em `src/modules/` ou `src/frontend/src/`
- ❌ **Nenhum `EnsureCreated`** encontrado em todo o `src/`

---

## 5. Resumo por Módulo

| Módulo | Stubs Relevantes | Impacto |
|---|---|---|
| AI & Knowledge | STUB-B01, B02, B03, B04, B05, B08, F01 | 🔴 Alto — módulo com mais stubs (25% maturity) |
| Catalog | STUB-B06, B07 | 🟠 Médio — graph vazio e fallback AI |
| Governance | STUB-F02 | ⚪ Baixo — widget message aceitável |
| Outros módulos | Nenhum stub relevante | ✅ Limpo |

---

## 6. Backlog de Ações

| ID | Ação | Prioridade | Estimativa |
|---|---|---|---|
| STUB-B03 | Implementar retrieval real no AI module | P1_CRITICAL | 40h |
| STUB-B04 | Implementar execução real de AI tools | P1_CRITICAL | 60h |
| STUB-B05 | Implementar streaming de respostas AI | P1_CRITICAL | 40h |
| STUB-B06 | Implementar CatalogGraphModuleService com dados reais | P2_HIGH | 16h |
| STUB-F01 | Substituir "coming soon" por funcionalidade real ou ocultar | P2_HIGH | 4h |
| STUB-B01 | Implementar health check real para AI sources | P3_MEDIUM | 8h |
| STUB-B02 | Migrar suggested prompts para configuração persistida | P3_MEDIUM | 4h |
| STUB-B07 | Manter fallback, adicionar logging quando AI indisponível | P3_MEDIUM | 2h |
