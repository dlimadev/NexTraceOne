# Plano 05 — AI Evolution (Fases 0–5)

> **Prioridade:** Média  
> **Esforço total:** 20–30 semanas  
> **Spec técnica:** [AI-EVOLUTION-ROADMAP.md](../AI-EVOLUTION-ROADMAP.md) + [AI-AGENT-LIGHTNING.md](../AI-AGENT-LIGHTNING.md)  
> **Contexto:** O módulo AIKnowledge tem base sólida (14 agentes, multi-provider, guardrails, grounding). Este plano evolui para sistema enterprise com RL, memória organizacional e marketplace.
> **Estado (Maio 2026):** Fase 0 PARCIAL (providers Ollama/OpenAI operacionais; Anthropic/LmStudio incompletos; grounding paralelo e Redis quota cache pendentes) | Fases 1–5 ROADMAP (não implementadas)

---

## Estado Atual (Diagnóstico)

| Componente | Estado | Gap |
|---|---|---|
| Multi-provider (Ollama, OpenAI, Anthropic, LmStudio) | Parcial | Anthropic e LmStudio incompletos |
| 14 agentes especializados | Estáticos | Não aprendem, não se adaptam |
| Tool calling (6 tools) | Implementado | Fallback text-based em modelos pequenos |
| 11 Guardrails com Sanitize | Implementado | Sem feedback loop de eficácia |
| Token quotas e budgets | Implementado | Sem cache Redis |
| Grounding cross-módulo (4 readers) | Implementado | Sem paralelismo (4 DB calls sequenciais) |
| MCP Integration (JSON-RPC 2.0) | Implementado | Base para Skills expansão |
| Modelos locais (deepseek-r1:1.5b, llama3.2:3b) | Operacional | Inadequados para produção enterprise |
| Sistema de Skills dinâmicas | **Ausente** | |
| Aprendizagem contínua (RL) | **Ausente** | |
| Memória organizacional | **Ausente** | |

---

## Fase 0 — Quick Wins (2–4 semanas)

### AI-0.1 — Completar Providers Existentes

**Anthropic provider:**
1. Finalizar `AnthropicProvider` em `AIKnowledge.Infrastructure`
2. Suporte a `claude-opus-4-7`, `claude-sonnet-4-6`, `claude-haiku-4-5` (modelos disponíveis em Abril 2026)
3. Streaming SSE via `IAsyncEnumerable<string>`
4. Tool calling nativo via Anthropic tool use API
5. Prompt caching (cache_control: ephemeral) para system prompts recorrentes — reduz custos ~60%

**LmStudio provider:**
1. `LmStudioProvider` usa API REST compatível com OpenAI format (`http://localhost:1234/v1`)
2. Model discovery automático via `GET /v1/models`

**Esforço:** 3–4 dias

### AI-0.2 — Redis Cache para Token Quotas

**Problema:** Verificações de quota fazem DB call por request → latência acumulada.

**Implementação:**
1. `ITokenQuotaCache` interface + `RedisTokenQuotaCache` implementação
2. Cache sliding window de 1min por utilizador/modelo
3. Fallback para DB se Redis indisponível
4. Config: `ai.quota.cache.ttl_seconds`, `ai.quota.cache.provider` (redis | memory)

**Esforço:** 1–2 dias

### AI-0.3 — Batch de Grounding Readers em Paralelo

**Problema:** 4 grounding readers executam sequencialmente → latência total = soma de 4 DB calls.

**Implementação:**
1. Refatorar `ContextAssembler` para usar `Task.WhenAll` nos 4 readers
2. Timeout por reader: 500ms (fail-open — reader indisponível não bloqueia resposta)
3. Cache de contexto por (`sessionId`, `timestamp`) com TTL 30s
4. Impacto esperado: redução ~40% no tempo de build de contexto

**Esforço:** 1 dia

### AI-0.4 — Upgrade de Modelos Locais

**Recomendação baseada em [AI-MODELS-ANALYSIS.md](../AI-MODELS-ANALYSIS.md):**
- **Primário:** `qwen2.5-coder:32b` (Apache 2.0 ✅) — 32B params, 131K context, tool calling nativo
- **Fallback CPU:** `qwen2.5-coder:14b` — 14B params, 64GB RAM
- **Reasoning:** `qwen2.5:72b` — 72B params, 48GB VRAM

**Implementação:**
1. Atualizar `ModelRegistry` com novos model IDs e capabilities
2. Routing: usar 32b para tool-use e análise complexa, 14b para summaries simples
3. Atualizar `docker-compose.yml`: adicionar volume para modelos Ollama
4. Documentação de setup em `docs/deployment/`

**Esforço:** 1–2 dias

### AI-0.5 — Native Tool Calling para Todos os Providers

**Problema:** Modelos Ollama sem native tool calling usam fallback text-based.

**Implementação:**
1. `INativeToolCallProvider` interface com `bool SupportsNativeToolCalls { get; }`
2. Para providers sem suporte: `StructuredOutputFallback` — solicita JSON estruturado com instruções explícitas no system prompt
3. `ToolCallRouter`: escolhe estratégia por provider capability
4. Garantir que os 14 agentes têm acesso consistente às 6 tools + novas tools de CC-05

**Esforço:** 2–3 dias

---

## Fase 1 — Agent Skills System (4–8 semanas)

**Objetivo:** Transformar os 14 agentes estáticos em capacidades dinâmicas, compostas e reutilizáveis.

### AI-1.1 — Skills Framework

**Implementação:**
1. Entidade `AgentSkill`:
   - `SkillId`, `Name`, `Description`, `Version`, `InputSchema` (JSON Schema), `OutputSchema`
   - `CapabilityTags[]` (ex: `["code-generation", "dotnet", "api-design"]`)
   - `IsBuiltIn`, `TenantId` (null = global)
2. `ISkillRegistry`: catalog de skills disponíveis com discovery por capability
3. `ISkillExecutor`: executa skill dado input + context
4. MCP integration: cada skill exposta como MCP tool (JSON-RPC 2.0)

### AI-1.2 — Built-in Skills (6 skills)

1. **`contract-reviewer`**: revê contrato API e gera QualityScore + issues (já existe como `AIContractReviewer` — adaptar para framework)
2. **`service-scaffolder`**: gera código de serviço (.NET/Java/Go/Python) a partir de contrato
3. **`incident-summarizer`**: resume incidente com root cause + timeline + mitigação
4. **`release-notes-generator`**: gera release notes por persona (já existe — adaptar)
5. **`blast-radius-explainer`**: explica impacto de mudança em linguagem natural
6. **`runbook-generator`**: propõe runbook a partir de incidente resolvido (já existe — adaptar)

### AI-1.3 — Skill Composition

**Implementação:**
1. `SkillPipeline`: sequência de skills onde output de uma é input da próxima
2. `SkillOrchestrator`: agente de alto nível que decompõe task em skills via planning
3. Limite: máximo 5 skills por pipeline (configurável)
4. Auditoria: cada step do pipeline auditado em `AiAgentExecution`

### AI-1.4 — Skills UI

1. `AiSkillsMarketplacePage.tsx` (`/ai/skills`): catálogo de skills disponíveis
2. `AiSkillExecutePage.tsx` (`/ai/skills/:id/execute`): formulário de input + output visual
3. Skill usage analytics no módulo `ProductAnalytics`

---

## Fase 2 — Agent Lightning (RL) (6–10 semanas)

**Objetivo:** Adicionar Reinforcement Learning para que agentes melhorem continuamente.  
**Framework:** Agent Lightning (Microsoft Research, Agosto 2025) — open-source.  
**Spec técnica:** [AI-AGENT-LIGHTNING.md](../AI-AGENT-LIGHTNING.md)

### AI-2.1 — Feedback Endpoint

**Implementação:**
1. Entidade `AiSkillFeedback`: (`ExecutionId`, `Rating` 1–5, `Comment`, `OutcomeType`: Correct/Partial/Wrong/Harmful, `ReviewedBy`)
2. Endpoint `POST /api/v1/ai/executions/{id}/feedback`
3. Frontend: thumbs up/down + rating modal pós-resposta do assistente

### AI-2.2 — Trajectory Exporter

**Implementação:**
1. `TrajectoryExportService`: background service que exporta `AiAgentExecution` (trajectórias completas) para Agent Lightning trainer
2. Export format: JSONL com campos `prompt`, `response`, `tool_calls`, `reward_signal`, `outcome`
3. `TrajectoryExportJob` (Quartz, diário): exporta trajectórias dos últimos 24h com feedback disponível

### AI-2.3 — Agent Lightning Python Service

**Implementação (novo serviço Python):**
```
tools/agent-lightning/
├── trainer.py          # RL trainer (GRPO/PPO)
├── reward_computer.py  # Calcula reward a partir de feedback
├── prompt_optimizer.py # Optimiza system prompts (APO)
└── docker-compose.yml  # Container isolado
```

1. `TrajectoryConsumer`: lê JSONL exportado pelo backend
2. `RewardComputer`: `rating >= 4` → +1, `rating <= 2` → -1, `outcome=Harmful` → -2
3. `RLTrainer`: treina modelos Ollama locais via GRPO (para modelos <32B) ou APO (para modelos externos)
4. Integração: modelo melhorado é re-registado em `ModelRegistry` com versão incrementada

### AI-2.4 — Agentes Prioritários para RL

| Agente | Feedback Source | Melhoria Esperada |
|--------|----------------|-------------------|
| `incident-responder` | Resolução real do incidente | MTTR -40% |
| `change-advisor` | Pós-deployment review | Falsos positivos -50% |
| `contract-reviewer` | Revisões aprovadas/rejeitadas | Iterações de review -45% |

---

## Fase 3 — Organizational Memory (4–6 semanas)

**Objetivo:** Agentes que lembram contexto de interações passadas e decisões organizacionais.

### AI-3.1 — Memory Store

**Implementação:**
1. Entidade `AiMemoryEntry`: (`TenantId`, `Scope`: Global/Team/User/Service, `Content`, `Embedding` pgvector, `RelevanceDecay`, `CreatedAt`, `LastAccessedAt`)
2. `IMemoryStore` interface: `StoreAsync`, `SearchAsync` (similarity search via pgvector), `ForgetAsync`
3. Embedding via modelo local (nomic-embed-text via Ollama)
4. Decay automático: entropia aumenta com tempo + inversamente proporcional ao uso

### AI-3.2 — Automatic Memory Formation

**Implementação:**
1. Após cada conversação bem avaliada (rating ≥ 4): extrair "decisões" e "factos" como memories
2. `MemoryExtractionService`: LLM pipeline que identifica informação reutilizável da conversa
3. Categorias de memória: `ArchitecturalDecision`, `ServiceContext`, `TeamPreference`, `IncidentPattern`

### AI-3.3 — Context Injection from Memory

**Implementação:**
1. `MemoryGroundingReader`: novo grounding reader que injeta memories relevantes no contexto
2. Ranking por similaridade (cosine) + recência + scope relevância
3. Limite: máximo 10 memories por contexto (para não exceder context window)
4. Memórias visíveis ao utilizador: `GET /api/v1/ai/memories` com filtro por scope

---

## Fase 4 — Enterprise Integrations (4–6 semanas)

### AI-4.1 — Agentic Runtime Governado (evolução da Wave Y)

**Evolução sobre Wave Y.1 (já implementado):**
1. Multi-step plans com aprovação humana para ações de alto risco (blast radius > threshold)
2. `AgentExecutionPlan` entity: decomposição em steps com `RequiresApproval` por step
3. `ApproveAgentPlanStep` command: Platform Admin aprova/rejeita steps críticos
4. Budget por plano: limite de tokens e custo por execução completa

### AI-4.2 — Cross-Module AI Tools (expansão)

**6 novas tools além das 3 existentes:**
1. `get_compliance_status` — verifica compliance de serviço para standard
2. `get_cost_context` — retorna custo atual e baseline de serviço
3. `get_knowledge_docs` — pesquisa na Knowledge Base por query semântica
4. `trigger_blast_radius` — calcula blast radius de uma mudança proposta
5. `get_evidence_pack` — lista evidence packs de uma release
6. `check_slo_status` — verifica estado de SLO de um serviço

### AI-4.3 — Model Cost Attribution Cross-Module

**Implementação:**
1. Cruzar `ExternalAiDbContext` (tokens usados) com `CostIntelligenceModule` (custo por serviço/equipa)
2. `GetAiCostAttributionReport` query: custo de IA por serviço, equipa, agente, modelo
3. Budget gates por equipa/serviço: alerta quando IA cost > threshold
4. Endpoint: `GET /api/v1/ai/cost-attribution`

---

## Fase 5 — AI Platform Marketplace (4–6 semanas)

### AI-5.1 — External Agent Connectors

**Implementação:**
1. `ExternalAgentConnector` entity: registo de agentes externos (via MCP) com endpoint, auth, capabilities
2. `CallExternalAgent` tool: permite agente interno delegar para agente externo registado
3. Auditoria completa de chamadas inter-agente via `AgentQueryRecord`

### AI-5.2 — Prompt Asset Registry

**Implementação:**
1. Entidade `PromptAsset`: system prompt versionado com eval set associado
2. `PromptVersion` entity: histórico de versões com métricas de qualidade por version
3. `ComparePromptVersions` query: A/B comparison de prompts no mesmo eval dataset
4. UI: `PromptRegistryPage.tsx` com diff visual entre versões

### AI-5.3 — AI Marketplace UI

**Implementação:**
1. `AiMarketplacePage.tsx`: catálogo de skills + agentes externos + connectors
2. Install/uninstall de skills por tenant
3. Rating e reviews por skill (aggregado, não individual)
4. Usage stats: execuções, latência média, satisfaction score por skill

---

## Critérios de Aceite Globais (estado Maio 2026)

- [ ] Fase 0: todos os 4 providers operacionais + grounding 40% mais rápido
  - Ollama e OpenAI: operacionais. Anthropic e LmStudio: incompletos (AI-0.1 pendente).
  - Grounding paralelo (Task.WhenAll): pendente (AI-0.3 pendente).
  - Redis quota cache: pendente (AI-0.2 pendente).
- [ ] Fase 1: 6 built-in skills executáveis via UI e API (AgentSkill framework — roadmap)
- [ ] Fase 2: `incident-responder` melhora accuracy mensurável após 30 dias de RL (roadmap)
- [ ] Fase 3: agente recorda decisões arquitecturais de conversações anteriores (roadmap)
- [ ] Fase 4: multi-step plan com aprovação humana funcional (roadmap)
- [ ] Fase 5: marketplace com 6+ skills instaláveis por tenant (roadmap)
