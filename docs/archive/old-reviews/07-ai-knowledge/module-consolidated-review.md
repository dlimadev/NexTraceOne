# AI Knowledge — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **AI Knowledge** é o pilar de inteligência artificial do NexTraceOne, organizado em 4 subdomínios de backend:

| Subdomínio | Propósito | DbContext |
|---|---|---|
| **ExternalAI** | Providers de IA externa, knowledge queries | `ExternalAiDbContext` (4 DbSets) |
| **Governance** | Model registry, políticas de acesso, budgets, routing, IDE | `AiGovernanceDbContext` (19+ DbSets) |
| **Runtime** | Execução de queries de IA | (partilhado) |
| **Orchestration** | Conversas, assistente, agentes, knowledge capture | `AiOrchestrationDbContext` (4 DbSets) |

| Métrica | Valor |
|---|---|
| Prioridade | P4 — Calibrar Expectativas |
| Maturidade global | **43%** |
| Backend | 🔴 **25%** (mais fraco de toda a plataforma) |
| Frontend | 🟡 70% (11 páginas, ~4 600 linhas) |
| Documentação | 🟡 65% (excessivamente otimista) |
| Testes | 🔴 **10%** (~5 testes reais) |
| Ficheiros C# backend | 278 |
| Endpoints REST | 54+ |
| Features CQRS | 58+ |
| Entidades de domínio | 40+ |
| Funções API frontend | 44 |
| Chaves i18n | 100+ |
| Agentes oficiais | 10 (semeados) |
| Providers reais | 2 (Ollama activo, OpenAI inactivo) |
| Migrações | 9 (7 Governance, 1 Orchestration, 1 ExternalAi) |

---

## 2. Estado Atual

### Pontuação de alinhamento com produto: 71% (BEM_ALINHADO)

O módulo tem uma **arquitectura sólida e genuinamente funcional** na camada de governança e inferência, mas apresenta um **desfasamento severo entre camadas**: o frontend exibe capacidades que o backend não suporta completamente.

### O que funciona de facto

| Capacidade | Estado | Evidência principal |
|---|---|---|
| Chat com inferência real | ✅ Funcional | `AiAssistantPage.tsx` (1 216 linhas), `ExecuteAiChat`, HTTP POST real para Ollama/OpenAI |
| Governança de modelos | ✅ Funcional | `AIModel` (40+ propriedades), `AiProvider`, `AIAccessPolicy` scope-based |
| Catálogo de agentes | ✅ Funcional | 10 agentes oficiais, `AiAgentsPage.tsx` (711 linhas), criação por utilizador |
| Execução real de agentes | ✅ Funcional | `AiAgentRuntimeService` — pipeline de 12 passos com inferência real |
| Auditoria completa | ✅ Funcional | `AIUsageEntry`, `AiExternalInferenceRecord`, `AiRoutingDecision`, `AiAuditPage.tsx` |
| Políticas e quotas | ✅ Funcional | `AIAccessPolicy`, `AiTokenQuotaPolicy`, `AiTokenUsageLedger` |
| IA interna como padrão | ✅ Conforme | Ollama local activo; IA externa requer configuração explícita |

### O que é parcial ou cosmético

| Capacidade | Estado | Problema concreto |
|---|---|---|
| Execução de tools | ❌ Cosmético | `AllowedTools` declarado em `AiAgent` mas **nenhum framework executa tools em runtime** |
| Streaming | ❌ Não implementado | Chat é request/response; `SupportsStreaming` existe como flag mas sem implementação |
| Contexto/grounding | ⚠️ Parcial | 5 toggles na UI (Services, Contracts, Incidents, Changes, Runbooks); injecção real no prompt **não confirmada** |
| Serviços de retrieval (RAG) | ⚠️ Possivelmente stubs | `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` registados mas funcionalidade incerta |
| IDE integrations | ⚠️ DB + UI sem extensão real | `IdeIntegrationsPage.tsx` (418 linhas) e entidades BD existem; **nenhuma extensão VS Code/Visual Studio no repositório** |
| Registo de modelos via UI | ⚠️ Botão desactivado | `ModelRegistryPage.tsx` — "Register Model" é placeholder; backend suporta |
| AssistantPanel | ⚠️ Mock | `AssistantPanel.tsx` tem "Mock contextual response generator" — **não afecta chat principal** |
| Knowledge capture | ⚠️ Parcial | `KnowledgeCaptureEntry` existe na BD; sem UI nem workflow claro |

---

## 3. Problemas Críticos e Bloqueadores

Agrupados por causa raiz para evitar redundância.

### 🔴 CR-1: Backend a 25% — desfasamento radical com frontend (70%)

O frontend apresenta 11 páginas funcionais e >4 600 linhas de UI, mas o backend que as alimenta está largamente incompleto. Esta é a **causa raiz principal** de quase todos os problemas do módulo: a decisão de construir UI antes de estabilizar o backend criou uma ilusão de maturidade.

**Sintomas:**
- Páginas com UI completa que invocam endpoints com stubs
- Documentação (AI-ARCHITECTURE.md, AI-ASSISTED-OPERATIONS.md, AI-DEVELOPER-EXPERIENCE.md) descreve sistema completo que não existe
- Testes a 10% — sem cobertura do backend incompleto

### 🔴 CR-2: Tools declarados mas COSMETIC_ONLY

Campo `AllowedTools` em `AiAgent` é preenchido nos 10 agentes oficiais mas **nenhum código interpreta ou executa tools em runtime**. `AiAgentRuntimeService` ignora o campo durante o pipeline de 12 passos.

**Impacto funcional directo:**
- Agentes geram apenas texto genérico sem acesso a dados reais
- "Analisa saúde do serviço X" → texto hipotético, não dados reais
- "Investiga incidente Z" → análise sem consultar logs/métricas
- "Gera contrato para API Y" → contrato sem schema existente

**Falta implementar:** `IToolRegistry`, `IToolExecutor`, `IToolPermissionService`, integração com providers (tool calling), parsing de tool calls, loop de execução, sandboxing, auditoria por tool.

### 🔴 CR-3: Documentação excessivamente otimista

6+ documentos (AI-ARCHITECTURE.md, AI-GOVERNANCE.md, AI-DEVELOPER-EXPERIENCE.md, AI-ASSISTED-OPERATIONS.md, etc.) prometem sistema completo multi-camada. A realidade é ~25% de implementação backend.

| Documento | Promete | Realidade |
|---|---|---|
| AI-ARCHITECTURE.md | Sistema multi-camada completo | ~20-25% implementado |
| AI-DEVELOPER-EXPERIENCE.md | IDE extensions (VS Code, Visual Studio) | Apenas página UI, zero extensões reais |
| AI-ASSISTED-OPERATIONS.md | 3 tipos de IA operacional | Implementação básica |
| AI-GOVERNANCE.md | Framework completo de controle | Governance funcional, orchestration com stubs |

---

## 4. Problemas por Camada

### 4.1 Frontend

| Severidade | Problema | Ficheiro/Rota |
|---|---|---|
| 🟠 Alto | 9 itens no menu para módulo parcial — sobre-representação | Sidebar AI Hub |
| 🟠 Alto | `AssistantPanel.tsx` com mock de respostas ("Mock contextual response generator") | `AssistantPanel.tsx` |
| 🟡 Médio | `ModelRegistryPage.tsx` — botão "Register Model" desactivado | `/ai/models` |
| 🟡 Médio | `IdeIntegrationsPage.tsx` (418 linhas) sem extensão real para utilizar | `/ai/ide` |
| 🟡 Médio | Texto i18n enganador: `"assistantEmptyTitle": "AI Assistant coming soon"` — feature funciona | i18n keys |
| 🟡 Médio | Toggles de contexto (Services, Contracts, Incidents, Changes, Runbooks) sem confirmação de grounding real | `AiAssistantPage.tsx` |
| 🟢 Baixo | Sem suporte a edição/regeneração de mensagens no chat | `AiAssistantPage.tsx` |

**Páginas existentes (11):**

| Página | Rota | Linhas | Estado |
|---|---|---|---|
| AiAssistantPage | `/ai/assistant` | 1 216 | ⚠️ Parcial (chat real, contexto incerto) |
| AiAgentsPage | `/ai/agents` | 711 | ⚠️ Parcial (tools cosméticos) |
| AgentDetailPage | `/ai/agents/:id` | 563 | ⚠️ Parcial |
| ModelRegistryPage | `/ai/models` | 243 | ⚠️ Parcial (registo desactivado) |
| AiPoliciesPage | `/ai/policies` | 185 | ⚠️ Parcial |
| AiRoutingPage | `/ai/routing` | 462 | ⚠️ Parcial |
| IdeIntegrationsPage | `/ai/ide` | 418 | ⚠️ Preview |
| TokenBudgetPage | `/ai/budgets` | 196 | ⚠️ Parcial |
| AiAuditPage | `/ai/audit` | 240 | ⚠️ Parcial |
| AiAnalysisPage | `/ai/analysis` | — | ⚠️ Preview |
| AiIntegrationsConfigurationPage | `/platform/configuration/ai-integrations` | — | ✅ Funcional |

### 4.2 Backend

| Severidade | Problema | Componente |
|---|---|---|
| 🔴 Crítico | Tools não executados em runtime — `AiAgentRuntimeService` ignora `AllowedTools` | `AiAgentRuntimeService` |
| 🔴 Crítico | Streaming não implementado — chamadas HTTP síncronas aos providers | `OpenAiHttpClient`, `OllamaHttpClient` |
| 🔴 Crítico | Serviços de retrieval possivelmente stubs | `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` |
| 🟠 Alto | Montagem de contexto possivelmente parcial no `SendAssistantMessage` | `SendAssistantMessage` handler |
| 🟠 Alto | 3/4 providers inactivos — requerem API keys sem processo documentado | OpenAI, Azure OpenAI, Google Gemini |
| 🟠 Alto | Endpoint de embeddings não implementado — flag `SupportsEmbeddings` existe sem backend | `AIModel` |
| 🟡 Médio | `AiSourceRegistryService` tem stub de health check para conectores futuros | `AiSourceRegistryService` |
| 🟡 Médio | Sem execução encadeada de agentes (chaining) | `AiAgentRuntimeService` |
| 🟡 Médio | Knowledge capture (`KnowledgeCaptureEntry`) sem workflow claro | Orchestration subdomínio |
| 🟢 Baixo | Sem retry automático em falhas de execução de agentes | `AiAgentRuntimeService` |
| 🟢 Baixo | Sem timeout configurável por agente (usa timeout do provider) | `AiAgent` |

**Endpoint modules existentes (5):**

| Module | Propósito | Rotas (estimadas) |
|---|---|---|
| `ExternalAiEndpointModule` | Providers, chat externo | `/api/ai/external/providers`, `/api/ai/external/chat` |
| `AiGovernanceEndpointModule` | Models, policies, budgets, audit | `/api/ai/governance/models`, `…/policies`, `…/budgets`, `…/audit` |
| `AiIdeEndpointModule` | IDE integrations | `/api/ai/ide/extensions`, `…/context` |
| `AiOrchestrationEndpointModule` | Sessions, knowledge | `/api/ai/orchestration/sessions`, `…/knowledge` |
| `AiRuntimeEndpointModule` | Status, métricas | `/api/ai/runtime/status`, `…/metrics` |

### 4.3 Database

| Severidade | Problema | Componente |
|---|---|---|
| 🟡 Médio | 7 migrações no `AiGovernanceDbContext` incluindo fixes de TenantId — dívida técnica | Migrações |
| 🟡 Médio | Sem modelo de IDE Extensions no schema (entidades existem mas gap com código) | `AiGovernanceDbContext` |
| 🟡 Médio | `AiAgentExecution` e `AiAgentExecutionStep` podem estar ausentes do DbContext principal | Gaps reportados no database report |
| 🟢 Baixo | Sem modelo de AI Feedback Loop persistido | `AiGovernanceDbContext` |
| 🟢 Baixo | Zero check constraints — validação apenas no aplicativo | Schema |

**Schema forte:** `AiGovernanceDbContext` é o maior contexto do sistema (19+ DbSets) cobrindo modelos, providers, políticas, agentes, quotas, auditoria, conhecimento, prompts, guardrails e experimentação.

### 4.4 Segurança

| Severidade | Problema | Componente |
|---|---|---|
| 🟠 Alto | Sem permissões por ferramenta/tool — quando tools forem implementados, não haverá controlo granular | Modelo de permissões |
| 🟠 Alto | Sem permissões por agente — todos os agentes visíveis são executáveis por qualquer utilizador com `ai:assistant:read` | `AIAccessPolicy` |
| 🟡 Médio | `ai:governance:read` é monolítica — sem subdivisão (modelos vs políticas vs routing vs auditoria) | Permissões |
| 🟡 Médio | Sem permissão de aprovação de artefactos — qualquer pessoa pode aprovar/rejeitar | `AiAgentArtifact.ReviewStatus` |
| 🟡 Médio | Logs de auditoria na mesma BD sem storage imutável/write-once | `AIUsageEntry` em `AiGovernanceDbContext` |
| 🟡 Médio | Conteúdo de chat não classificado como dados sensíveis | `AiMessage` |
| 🟢 Baixo | Sem rate limiting temporal (quotas de tokens existem, sem limite por minuto/hora) | Autorização |

**Permissões existentes (4):**

| Permissão | Escopo |
|---|---|
| `ai:assistant:read` | Chat, agentes |
| `ai:governance:read` | Modelos, políticas, routing, budgets, auditoria, IDE |
| `ai:runtime:write` | Análise de ambiente |
| `platform:admin:read` | Configuração de IA |

**O que funciona bem:** Políticas scope-based (Role/User/Team/Tenant), controlo AllowedModelIds/BlockedModelIds, AllowExternalAI/InternalOnly, verificação via `IAiModelAuthorizationService`, `AiTokenQuotaPolicy` + `AiTokenUsageLedger`.

### 4.5 IA e Agentes

| Severidade | Problema | Componente |
|---|---|---|
| 🔴 Crítico | Tools COSMETIC_ONLY — lacuna funcional central do módulo | `AiAgent.AllowedTools` |
| 🔴 Crítico | Sem framework de tool calling (IToolRegistry, IToolExecutor, loop de execução) | Todo o pipeline |
| 🟠 Alto | Sem Semantic Kernel ou LangChain — inferência directa via HTTP sem framework de orquestração | Arquitectura |
| 🟠 Alto | Grounding/RAG possivelmente parcial — agentes podem não receber dados reais dos domínios | Context assembly |
| 🟡 Médio | Human-in-the-loop incompleto — `ReviewStatus` existe sem workflow formal (notificações, SLAs, escalonamento) | `AiAgentArtifact` |
| 🟡 Médio | Sem gestão de window de contexto (truncamento, sumarização de conversas longas) | Memória |
| 🟡 Médio | Sem memória cross-conversation | `AiAssistantConversation` |
| 🟢 Baixo | Variabilidade na inclusão de exemplos nos prompts dos agentes | System prompts |

**10 agentes oficiais semeados:**

| # | Nome | Categoria | Saída |
|---|---|---|---|
| 1 | Service Health Analyzer | IncidentResponse | Relatório de saúde |
| 2 | SLA Compliance Checker | ServiceAnalysis | Relatório SLA |
| 3 | Change Impact Evaluator | ChangeIntelligence | Análise de impacto |
| 4 | Incident Root Cause Investigator | IncidentResponse | Análise causa raiz |
| 5 | Security Posture Assessor | SecurityAudit | Avaliação segurança |
| 6 | Release Risk Evaluator | ChangeIntelligence | Avaliação de risco |
| 7 | API Contract Draft Generator | ApiDesign | OpenAPI 3.1 YAML |
| 8 | API Test Scenario Generator | TestGeneration | Cenários estruturados |
| 9 | Kafka Schema Contract Designer | EventDesign | Avro / JSON Schema |
| 10 | SOAP Contract Author | SoapDesign | WSDL |

**Comparação com frameworks de mercado:**

| Capacidade | NexTraceOne | Semantic Kernel / LangChain |
|---|---|---|
| Governança empresarial | ✅ Nativa | ❌ Requer extensão |
| Auditoria | ✅ Completa | ❌ Requer extensão |
| Tool calling | ❌ Cosmético | ✅ Nativo |
| Streaming | ❌ | ✅ |
| Chaining de agentes | ❌ | ✅ |

### 4.6 Documentação

| Severidade | Problema |
|---|---|
| 🔴 Crítico | 6+ documentos descrevem features como implementadas que estão a 20-25% — cria expectativas falsas |
| 🟠 Alto | Sem README do módulo — impossível onboarding de developer |
| 🟠 Alto | Sem guia de activação de providers (API keys, configuração) |
| 🟡 Médio | Classes sem documentação adequada (conforme code-comments-review) |
| 🟡 Médio | Interfaces e complex methods sem JSDoc/XML docs |

---

## 5. Dependências

### Dependências de entrada (este módulo depende de)

| Módulo | Tipo | Propósito | Criticidade |
|---|---|---|---|
| **Identity & Access** | Runtime | Autenticação JWT, permissões, tenant | 🔴 Crítica |
| **Catalog** | Dados | Serviços para contexto dos agentes (toggle Services) | 🟠 Alta |
| **Contracts** | Dados | Contratos para contexto e geração (toggle Contracts) | 🟠 Alta |
| **Operations** | Dados | Incidentes, runbooks (toggles Incidents, Runbooks) | 🟡 Média |
| **Change Governance** | Dados | Mudanças em produção (toggle Changes) | 🟡 Média |

### Dependências de saída (outros módulos dependem deste)

| Módulo | Tipo | Propósito |
|---|---|---|
| Contracts (Contract Studio) | Geração | Geração de contratos via agentes de IA |
| Transversal | Assistência | Chat contextual via `AssistantPanel` (actualmente mock) |

### Nota sobre isolamento

As dependências de dados (Catalog, Contracts, Operations, Changes) são críticas para o grounding/RAG. Sem estas integrações, os agentes operam sem contexto real do produto — que é precisamente o gap actual.

---

## 6. Quick Wins

Acções de baixo esforço e alto impacto que podem ser realizadas sem refactoring estrutural.

| # | Acção | Esforço | Impacto | Ref |
|---|---|---|---|---|
| QW-1 | **Corrigir mensagem i18n** `"assistantEmptyTitle": "AI Assistant coming soon"` para texto adequado | 30 min | UX | QW-5 |
| QW-2 | **Corrigir documentação AI** — adicionar indicadores de maturidade real (%) em todos os docs | 3h | Transparência | QW-9 |
| QW-3 | **Decidir se IdeIntegrationsPage deve continuar no menu** ou ser ocultada até existirem extensões reais | 30 min | UX | module-review #3 |
| QW-4 | **Reduzir menu de 9 para 5-6 itens** — agrupar governance items (Policies + Routing + Budgets → "AI Governance") | 1h | UX | module-review #7 |
| QW-5 | **Criar README do módulo** com setup, arquitectura, estado real e limitações conhecidas | 4h | Onboarding | CR-2 docs |
| QW-6 | **Documentar processo de activação de providers** (API keys OpenAI, Azure, Gemini) | 2h | Operação | GAP-06 |
| QW-7 | **Activar botão "Register Model"** na `ModelRegistryPage.tsx` — backend já suporta | 4h | Funcionalidade | GAP-01 |
| QW-8 | **Actualizar texto i18n de empty states** para reflectir capacidades reais do chat | 2h | UX | QW-5 |
| QW-9 | **Marcar features não implementadas como "Planeado"** nos docs de AI | 3h | Transparência | QW-9 |

**Esforço total estimado:** ~3 dias

---

## 7. Refactors Estruturais

Transformações que requerem planeamento, implementação significativa e testes extensivos.

| # | Refactor | Esforço | Impacto | Prioridade | Risco |
|---|---|---|---|---|---|
| SR-1 | **Implementar execução de tools** — `IToolRegistry`, `IToolExecutor`, integração com pipeline, sandboxing | 2-3 semanas | 🔴 Transformacional | P3 | Médio-Alto (side-effects) |
| SR-2 | **Implementar streaming** para chat e agentes — SSE ou WebSocket, UI incremental | 2-3 semanas | 🟠 Alto | P3 | Médio |
| SR-3 | **Elevar backend de 25% para 50%+** — completar handlers core, validações, integrações | 3-4 semanas | 🟠 Alto | P3 | Médio-Alto |
| SR-4 | **Implementar RAG/Retrieval real** — completar `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` | 3-4 semanas | 🟠 Alto | P3 | Alto |
| SR-5 | **Segmentar permissões** — dividir `ai:governance:read`, adicionar permissões por agente e por tool | 1-2 semanas | 🟡 Médio | P3 | Baixo |
| SR-6 | **Workflow de aprovação formal** — integrar `AiAgentArtifact.ReviewStatus` com notificações, SLAs, escalonamento | 1-2 semanas | 🟡 Médio | P4 | Baixo |
| SR-7 | **Consolidar migrações** — reduzir 7 migrações em `AiGovernanceDbContext` | 3 dias | 🟢 Baixo | P4 | Baixo |
| SR-8 | **Avaliar adopção de Semantic Kernel** — para tool calling e chaining sem reescrever governança | 1 semana (avaliação) | 🟡 Médio | P3 | Médio |

**Cadeia de dependências:**

```
SR-1 (Tools) ──────────► SR-4 (RAG) ──► SR-5 (Permissões tools)
SR-3 (Backend 50%+) ────► SR-2 (Streaming) pode executar em paralelo
SR-8 (Semantic Kernel) ─► Informa SR-1 (deve ser avaliado antes)
```

---

## 8. Critérios de Fecho

Conforme definido no `module-closure-plan.md`, com refinamentos da auditoria estrutural.

### Mínimos obrigatórios (maturidade ≥65%)

| # | Critério | Métrica | Estado actual |
|---|---|---|---|
| 1 | Backend acima de 50% | Completude de handlers core | 🔴 25% |
| 2 | Tools conectados em runtime | ≥3 tools executam acções reais | 🔴 0 tools |
| 3 | Streaming funcional | Chat com respostas incrementais | 🔴 Não existe |
| 4 | ≥2 providers activos | Ollama + ≥1 cloud provider | 🟡 1 activo |
| 5 | Documentação alinhada com realidade | Zero features "fantasma" nos docs | 🔴 6+ docs otimistas |
| 6 | README do módulo criado | Guia de setup e limitações | 🔴 Não existe |
| 7 | Testes ≥40% | Cobertura de backend e integração | 🔴 10% |

### Verificação por etapa (do closure plan)

| Etapa | Descrição | Estado |
|---|---|---|
| 1. Security/Permission | Permissões por persona, token budgets enforced, auditoria de prompts | 🟡 Parcial |
| 2. Persistence/Backend | Backend 50%+, tools reais, providers conectados, model registry | 🔴 Não iniciado |
| 3. Frontend Integration | Streaming UI, tool results na UI, estado real de providers | 🔴 Não iniciado |
| 4. UX/i18n | Chat i18n, empty states, loading states para streaming | 🟡 Parcial |
| 5. Documentation | Docs honestos, providers documentados, tools e limitações | 🔴 Não iniciado |
| 6. AI/Agents | Tools runtime, RAG básico, 3 agentes com execução real | 🔴 Não iniciado |
| 7. Checklist Final | Todos os critérios acima satisfeitos | 🔴 Não iniciado |

---

## 9. Plano de Ação Priorizado

### Fase 0 — Quick Wins imediatos (1 semana)

| Ordem | Acção | Esforço |
|---|---|---|
| 1 | Calibrar documentação com indicadores de maturidade real | 3h |
| 2 | Criar README do módulo | 4h |
| 3 | Documentar processo de activação de providers | 2h |
| 4 | Corrigir textos i18n enganadores no chat e empty states | 2h |
| 5 | Decidir sobre IdeIntegrationsPage e reduzir menu | 1h |

### Fase 1 — Avaliação e decisão (1 semana)

| Ordem | Acção | Esforço |
|---|---|---|
| 6 | Avaliar adopção de Semantic Kernel vs implementação directa de tools | 1 semana |
| 7 | Validar se serviços de retrieval são stubs ou funcionais | 2h |
| 8 | Validar montagem de contexto end-to-end (toggle → prompt) | 2h |
| 9 | Definir MVP de AI — funcionalidades mínimas para produção | 4h |

### Fase 2 — Backend core (3-4 semanas, Wave 4)

| Ordem | Acção | Esforço |
|---|---|---|
| 10 | Implementar `IToolRegistry` + `IToolExecutor` com ≥3 tools reais | 2-3 semanas |
| 11 | Activar provider OpenAI (configuração API key) | 2-3 dias |
| 12 | Activar provider Azure AI | 2-3 dias |
| 13 | Elevar backend de 25% para 50%+ (handlers core) | 3-4 semanas |
| 14 | Activar botão "Register Model" na UI | 4h |

### Fase 3 — UX e integração (2-3 semanas)

| Ordem | Acção | Esforço |
|---|---|---|
| 15 | Implementar streaming (SSE) para chat | 2-3 semanas |
| 16 | Completar serviços de retrieval para RAG básico | 2-3 semanas |
| 17 | Conectar tool results à UI dos agentes | 1 semana |
| 18 | Substituir mock do AssistantPanel por integração real | 3-5 dias |

### Fase 4 — Segurança e qualidade (1-2 semanas)

| Ordem | Acção | Esforço |
|---|---|---|
| 19 | Segmentar permissões (por agente, por tool, subdivisão governance) | 1-2 semanas |
| 20 | Aumentar testes de 10% para 40%+ | 2-3 semanas |
| 21 | Implementar exportação de auditoria (CSV/JSON) | 3-5 dias |

### Marco de fecho estimado

- **Quick Wins:** Semana 1
- **Backend funcional (tools + providers):** Semana 4-5
- **Streaming + RAG:** Semana 7-8
- **Maturidade ≥65%:** Semana 10-12

---

## 10. Inconsistências entre Relatórios

A análise cruzada dos relatórios revela inconsistências significativas que devem ser resolvidas.

### 10.1 Maturidade global discrepante

| Fonte | Maturidade reportada |
|---|---|
| `module-review.md` | ~20-25% (foco no backend) |
| `module-consolidation-report.md` | 43% (média ponderada: backend 25%, frontend 70%, docs 65%, testes 10%) |
| `ai-and-agents-structural-audit.md` | **75-80%** (foco na arquitectura e funcionalidades existentes) |
| `ai-product-alignment-report.md` | 71% (alinhamento com produto) |

**Análise:** A discrepância de 75-80% (auditoria estrutural) vs 25% (backend) vs 43% (consolidação) resulta de critérios diferentes. A auditoria estrutural conta ficheiros, entidades e endpoints existentes; a revisão modular avalia funcionalidade real end-to-end. **O valor de 43% é o mais representativo** porque pondera todas as camadas. O backend a 25% é o número mais relevante operacionalmente.

### 10.2 Classificação do chat divergente

| Fonte | Classificação do chat |
|---|---|
| `module-review.md` | "⚠️ Parcial — UI completa, backend com stubs de LLM" |
| `ai-chat-audit-report.md` | "FUNCIONAL_APARENTE — inferência real executada" |
| `ai-and-agents-structural-audit.md` | "✅ Funcional — inferência real via Ollama/OpenAI" |

**Análise:** O chat principal (`AiAssistantPage.tsx`) executa **inferência real** via `ExecuteAiChat` → `OpenAiHttpClient`/`OllamaHttpClient`. A classificação "stubs de LLM" no module-review é **incorrecta** para o chat principal — aplica-se apenas ao `AssistantPanel.tsx` (painel lateral). O chat é funcional; o contexto/grounding é que é parcial.

### 10.3 Número de páginas frontend

| Fonte | Páginas |
|---|---|
| `module-review.md` | 11 páginas |
| `ai-and-agents-structural-audit.md` | 12 páginas |

**Análise:** Diferença de 1 página — provavelmente a `AiIntegrationsConfigurationPage` em `/platform/configuration/` é por vezes contada separadamente.

### 10.4 "Cosmético" vs "Funcional"

O `root-cause-consolidation-report.md` classifica o módulo AI como tendo "CR-4: Módulo AI Cosmético na Execução de Ferramentas", enquanto a `ai-and-agents-structural-audit.md` classifica como "substancialmente implementado e real (75-80%)".

**Análise:** Ambos estão correctos em âmbitos diferentes. A **governança, inferência, auditoria e agentes são reais**. A **execução de tools é cosmética**. A documentação é excessivamente otimista. O label "cosmético" aplica-se à execução de tools e ao gap UI-backend, não ao módulo inteiro.

### 10.5 Linhas de AiAssistantPage.tsx

| Fonte | Linhas |
|---|---|
| `module-review.md` | 483 linhas |
| `ai-chat-audit-report.md` | 1 216 linhas |
| `ai-and-agents-structural-audit.md` | 1 216 linhas |

**Análise:** Valor de 1 216 linhas confirmado por duas fontes independentes e mais recentes. O valor de 483 pode referir-se a uma versão anterior ou a um componente diferente.

---

> **Veredicto final:** O módulo AI Knowledge tem uma **base arquitectural sólida e genuinamente funcional** em governança, inferência e auditoria. O problema central é o **desfasamento entre camadas** (frontend 70% vs backend 25%) e a **ausência de execução de tools** que limita os agentes a geradores de texto genérico. A documentação agrava o problema ao criar expectativas falsas. Com a execução de tools, streaming, activação de providers e documentação honesta, o módulo pode passar de 43% para ≥65% em ~10-12 semanas (Wave 4).
