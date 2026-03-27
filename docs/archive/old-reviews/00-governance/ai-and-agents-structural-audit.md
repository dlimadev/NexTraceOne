# Auditoria Estrutural — Módulo de IA e Agentes do NexTraceOne

> **Data:** 2025-07-15
> **Classificação geral:** FUNCIONAL com lacunas PARCIAIS — maturidade estimada 75–80 % para camada empresarial de IA.

---

## 1. Resumo executivo

O módulo de IA do NexTraceOne é **substancialmente implementado e real** — não se trata de scaffolding ou protótipo.

| Dimensão | Valor |
|---|---|
| Ficheiros C# backend | 278 |
| Páginas frontend | 12 |
| Endpoints REST | 54+ |
| Features CQRS | 58+ |
| Entidades de domínio | 40+ |
| Agentes oficiais semeados | 10 |
| Providers reais | 2 (OpenAI cloud + Ollama local) |
| DbContexts | 3 (AiGovernance 19 DbSets, AiOrchestration 4, ExternalAi 4) |
| Migrações | 9 (7 AiGovernance, 1 AiOrchestration, 1 ExternalAi) |
| Abstrações de repositório | 37 — todas com EF Core real (zero mocks) |
| Funções API frontend | 44 |
| Chaves i18n | 100+ |

### Avaliação de maturidade

| Capacidade | Maturidade | Notas |
|---|---|---|
| Chat de IA | ✅ Funcional | Inferência real via Ollama/OpenAI, persistência, metadados |
| Modelos e providers | ✅ Bem estruturado | 40+ propriedades por modelo, 4 providers semeados (1 activo) |
| Agentes | ✅ Funcional | 10 oficiais, criação pelo utilizador, execução real |
| Prompts/contexto | ⚠️ Parcial | Prompts estruturados, contexto parcialmente montado |
| Tools | ❌ Cosmético | AllowedTools declarado mas não executado |
| Permissões | ⚠️ Parcial | Acesso a modelos controlado; sem permissões por ferramenta |
| Auditoria | ✅ Funcional | Trilha completa por chamada, tokens, custo, correlação |
| Streaming | ❌ Não implementado | Request/response apenas |
| RAG/Retrieval | ⚠️ Parcial | Serviços registados, possivelmente stubs |
| IDE | ⚠️ Parcial | DB + UI existem, extensão real ausente do repositório |

### Conclusão-chave

A IA é **real e funcional**, com execução de inferência, persistência de conversas, governança de modelos, políticas de acesso, auditoria completa e 10 agentes operacionais. As lacunas principais são: execução de tools não ligada, streaming não implementado, 3/4 providers inactivos (requerem API keys), registo de modelos via UI desactivado, AssistantPanel com mock, serviços de retrieval possivelmente parciais, sem framework Semantic Kernel.

---

## 2. Chat de IA

### Estado: FUNCIONAL_APARENTE

| Evidência | Ficheiro |
|---|---|
| UI principal do chat | `src/frontend/…/AiAssistantPage.tsx` (1 216 linhas) |
| Chamadas API | `listConversations`, `getConversation`, `sendMessage`, `createConversation`, `checkProvidersHealth`, `listAvailableModels`, `listAgents` |
| Persistência | `AiAssistantConversation` + `AiMessage` em `AiGovernanceDbContext` |
| Selecção de modelo | Utilizador escolhe entre modelos agrupados por interno/externo com restrições de política |
| Metadados por mensagem | modelName, provider, isInternalModel, tokens, appliedPolicy, groundingSources, contextReferences, correlationId |
| Provider real — OpenAI | `OpenAiHttpClient` — HTTP POST para `/v1/chat/completions` com Bearer auth |
| Provider real — Ollama | `OllamaHttpClient` — POST para `/api/chat` com retry logic |
| Handler de execução | `ExecuteAiChat` — inferência real, guarda tokens na BD, regista auditoria, mede duração |

### Lacunas identificadas

- Sem streaming de respostas
- `AssistantPanel.tsx` (painel lateral contextual) tem gerador de respostas mock — **não é o chat principal**
- Montagem de contexto (`SendAssistantMessage`) pode ser parcial
- Texto i18n `"assistantEmptyTitle": "AI Assistant coming soon"` é o estado vazio, não reflecte o estado real da feature

---

## 3. Modelos e providers

### Estado: BEM_ESTRUTURADO com ACTIVAÇÃO PARCIAL

| Componente | Detalhes |
|---|---|
| `AIModel` | 40+ propriedades: Name, Provider, ModelType, Category, IsInternal/External, Status, Capabilities, SensitivityLevel, SupportsStreaming/ToolCalling/Vision/StructuredOutput, ContextWindow, RequiresGpu, ComplianceStatus |
| `AiProvider` | ProviderType, BaseUrl, IsLocal/External, AuthenticationMode (NoAuth/ApiKey/OAuth2/MutualTLS), HealthStatus, Priority, TimeoutSeconds |
| `AIAccessPolicy` | Scope-based (role/user/team/tenant), AllowedModelIds, BlockedModelIds, AllowExternalAI, InternalOnly, MaxTokensPerRequest, EnvironmentRestrictions |
| Providers semeados | 4 — Ollama (activo), OpenAI (inactivo), Azure OpenAI (inactivo), Google Gemini (inactivo) |
| Modelos semeados | 3 — DeepSeek R1 1.5B (activo/local), GPT-4o-mini (inactivo/externo), GPT-4o (inactivo/externo) |
| Routing | `AIRoutingStrategy` + `AIRoutingDecision` para selecção inteligente de modelo |
| Provider Factory | `IAiProviderFactory` resolve providers por ID; OpenAI condicional à existência de API key |

### Lacunas

- Apenas 1 modelo activo por defeito (DeepSeek R1 1.5B via Ollama)
- 3 providers inactivos requerem configuração de API keys
- `ModelRegistryPage.tsx` — botão "Register Model" **desactivado** (placeholder)
- Sem execução de embeddings (flag existe mas endpoint não implementado)

---

## 4. Agentes — catálogo, gestão e execução

### Estado: FUNCIONAL_APARENTE

#### 10 agentes oficiais semeados (todos System/Tenant/Published)

| # | Nome | Categoria |
|---|---|---|
| 1 | Service Health Analyzer | IncidentResponse |
| 2 | SLA Compliance Checker | ServiceAnalysis |
| 3 | Change Impact Evaluator | ChangeIntelligence |
| 4 | Incident Root Cause Investigator | IncidentResponse |
| 5 | Security Posture Assessor | SecurityAudit |
| 6 | Release Risk Evaluator | ChangeIntelligence |
| 7 | API Contract Draft Generator | ApiDesign |
| 8 | API Test Scenario Generator | TestGeneration |
| 9 | Kafka Schema Contract Designer | EventDesign |
| 10 | SOAP Contract Author | SoapDesign |

#### Entidade `AiAgent`

Name, DisplayName, Slug, Description, Category, SystemPrompt (até 10K chars), PreferredModelId, Capabilities, TargetPersona, Icon, OwnershipType (System/Tenant/User), Visibility (Private/Team/Tenant), PublicationStatus (Draft/PendingReview/Active/Published/Archived/Blocked), AllowedModelIds, AllowedTools, InputSchema, OutputSchema, AllowModelOverride, Version, ExecutionCount.

#### Execução real

- `AiAgentRuntimeService` — pipeline de 12 passos: resolver agente → validar → resolver modelo → resolver provider → construir prompt → **EXECUTAR INFERÊNCIA** → persistir execução → gerar artefactos
- `AiAgentExecution` — AgentId, RequesterId, Status, Input, Output, ModelIdUsed, Cost, Duration, StartedAt, CompletedAt, Error
- `AiAgentArtifact` — AgentId, ExecutionId, Type, Content, ReviewStatus (PendingReview/Approved/Rejected/Archived), ReviewedBy, ReviewedAt

#### Frontend

- `AiAgentsPage.tsx` (711 linhas) — catálogo, criação, execução, revisão de artefactos
- `AgentDetailPage.tsx` (563 linhas) — visualização, edição, execução
- Utilizador **pode criar agentes** via `createAgent` API

---

## 5. Prompts, contexto, memória e tools

### Prompts: ESTRUTURADOS

- Agentes oficiais têm prompts de sistema detalhados (800+ chars cada) com regras específicas e formato de saída
- SystemPrompt suporta até 10K caracteres

### Contexto: PARCIAL

- Toggles de contexto na UI do chat: Services, Contracts, Incidents, Changes, Runbooks
- Parsing de context bundle JSON em `SendAssistantMessage`
- Serviços RAG registados: `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` — interfaces existem, implementações registadas
- `AIKnowledgeSource` — Name, Type, Description, Url, Query, Weight, Priority, LastSyncAt
- Grounding real pode ser parcial

### Memória: FUNCIONAL

- Conversas + mensagens totalmente persistidas com histórico completo do thread

### Tools: COSMÉTICO_APENAS

- Campo `AllowedTools` existe na entidade `AiAgent`
- **Nenhum framework de execução de tools implementado** — tools são declarados mas não invocados em runtime
- Sem permissões por ferramenta

---

## 6. Segurança e permissões

### 4 permissões distintas de IA

| Permissão | Escopo |
|---|---|
| `ai:assistant:read` | Chat, agentes |
| `ai:governance:read` | Modelos, políticas, routing, budgets, auditoria, IDE |
| `ai:runtime:write` | Análise de ambiente |
| `platform:admin:read` | Configuração de IA |

### Controlos implementados

- `IAiModelAuthorizationService` — verificação baseada em políticas por modelo
- `AIAccessPolicy` — restrições por scope com AllowedModelIds, BlockedModelIds, AllowExternalAI, MaxTokensPerRequest
- `AiTokenQuotaPolicy` + `AiTokenUsageLedger` para rastreamento de consumo

### Lacunas

- Sem permissões por ferramenta/tool
- Modelo de capacidades PARCIALMENTE implementado (acesso a modelos + quotas de tokens + restrições de IA externa, mas sem capacidades granulares por ferramenta/acção)

---

## 7. Rastreabilidade e observabilidade

### Estado: RASTREÁVEL — trilha de auditoria abrangente para conformidade empresarial

| Entidade | Propósito |
|---|---|
| `AIUsageEntry` | Auditoria completa por chamada: userId, modelId, provider, isInternal, tokens, result (Allowed/Blocked/QuotaExceeded), clientType, policyName, contextScope, correlationId, conversationId |
| `AiExternalInferenceRecord` | Por chamada externa: ProviderId, ModelId, ExternalRequestId, InputTokens, OutputTokens, Cost, Duration, Success, Error |
| `AiRoutingDecision` | SourceContext, SelectedProvider, SelectedModel, Confidence, ExecutedAt, Cost, TokensUsed |
| `AiAgentExecution` | Ciclo de vida de execução de agente: status, tokens, custo, duração, erro |
| Metadados de conversa | Cada mensagem rastreia modelo, provider, tokens, política, grounding sources, correlation ID |
| Interceptor padrão | CreatedAt/By, UpdatedAt/By em todas as entidades |
| Soft delete | IsDeleted em todas as entidades |
| UI | `AiAuditPage.tsx` — trilha de auditoria completa com pesquisa, filtro por resultado, exibição de todos os metadados |

---

## 8. Aderência ao produto

O módulo de IA está **fortemente alinhado** com a visão oficial do NexTraceOne como fonte de verdade governada:

- ✅ IA interna local como padrão (Ollama)
- ✅ IA externa opcional e governada (OpenAI com controlo de política)
- ✅ Agentes especializados para domínios do produto (contratos, incidentes, mudanças, segurança)
- ✅ Auditoria completa de uso
- ✅ Quotas e budgets de tokens
- ✅ Controlo por utilizador/equipa/tenant
- ⚠️ Assistência contextual parcial (toggles existem, grounding pode ser parcial)
- ⚠️ Human-in-the-loop parcial (revisão de artefactos sem workflow de aprovação)
- ⚠️ Captura de conhecimento parcial (KnowledgeCaptureEntry existe, workflow incerto)
- ❌ Extensões IDE ausentes do repositório (DB + UI existem)

---

## 9. Cruzamento entre camadas

| Funcionalidade | UI | Backend | BD | Alinhamento |
|---|---|---|---|---|
| Chat principal | ✅ 1 216 linhas | ✅ Inferência real | ✅ Conversas persistidas | ✅ Alinhado |
| Registo de modelos | ⚠️ Botão desactivado | ✅ Suporta | ✅ Entidades completas | ⚠️ UI incompleta |
| AssistantPanel | ⚠️ Mock no sidebar | ✅ Chat real funciona | ✅ | ⚠️ Sidebar mock |
| Execução de tools | ⚠️ UI declara | ⚠️ Declarado mas não executado | ✅ Armazenado | ❌ Não funcional |
| Integrações IDE | ✅ Página completa | ✅ Registo de clientes | ✅ Entidades existem | ⚠️ Extensão real ausente |
| Streaming | ❌ | ❌ | N/A | ❌ Não implementado |
| 3 providers inactivos | ✅ Listados | ✅ Configurados | ✅ Semeados | ⚠️ Requerem API keys |

---

## 10. Recomendações

### Prioridade Alta

1. **Implementar execução de tools** — ligar AllowedTools à execução real no `AiAgentRuntimeService`
2. **Activar providers adicionais** — documentar processo de configuração de API keys para OpenAI/Azure/Gemini
3. **Completar montagem de contexto** — garantir que toggles de Services/Contracts/Incidents/Changes/Runbooks realmente injectam dados no prompt
4. **Habilitar registo de modelos** — activar botão "Register Model" na `ModelRegistryPage.tsx`

### Prioridade Média

5. **Implementar streaming** — melhorar experiência de chat com respostas incrementais
6. **Remover mock do AssistantPanel** — substituir gerador de respostas mock por integração real
7. **Validar serviços de retrieval** — confirmar que `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` são funcionais
8. **Workflow de aprovação de artefactos** — integrar revisão de artefactos com workflow de aprovação

### Prioridade Baixa

9. **Extensões IDE** — desenvolver extensões reais para VS Code e Visual Studio
10. **Framework Semantic Kernel** — avaliar adopção para funcionalidades avançadas (tool calling, planning)
11. **Endpoint de embeddings** — implementar execução real de embeddings
12. **Consolidar migrações** — resolver dívida de 7 migrações no AiGovernanceDbContext

---

> **Veredicto final:** O módulo de IA do NexTraceOne é **genuinamente funcional** com infraestrutura sólida de governança, execução real de inferência, agentes operacionais e auditoria completa. A maturidade de 75–80 % posiciona-o bem para evolução incremental focada nas lacunas identificadas.
