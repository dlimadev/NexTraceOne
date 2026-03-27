# Relatório de IA, Agentes e Governança de IA — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Objetivo no Contexto do Produto

A IA no NexTraceOne deve ser uma capacidade governada, auditável, contextualizada ao produto e útil para as personas. Não pode ser um chat genérico. Deve: operar dentro de políticas, respeitar limites por utilizador/grupo/tenant, usar fontes de conhecimento do produto (contratos, mudanças, incidentes) e ser auditável.

---

## 2. Estado Geral do Módulo AI Knowledge

### Resumo por Área

| Área | DbContext | Migrações | Features | Estado Real |
|---|---|---|---|---|
| AI Governance | AiGovernanceDbContext | Sim | 28 | PARTIAL — repositórios EF Core funcionais |
| AI Orchestration | AiOrchestrationDbContext | Snapshot (sem confirmação) | Parcial | PARTIAL |
| External AI | ExternalAiDbContext | Snapshot (sem confirmação) | 8 TODO stubs | STUB |

**Total features módulo:** ~36 features, 78% real, 22% stub/plan

---

## 3. AI Governance — Estado

**Status: PARTIAL/READY**

### O que funciona:
- **Model Registry**: CRUD de modelos, tracking de budget, metadata
- **AI Access Policies**: Políticas por utilizador e por grupo
- **Token & Budget Governance**: Controlo de gastos de tokens por tenant/utilizador
- **AI Audit**: Registo de uso de IA

### O que falta:
- `IAiOrchestrationModule` = PLAN (interface vazia, sem métodos implementados)
- A governança existe mas o assistant não a consulta de facto (porque o assistant é mock)

**Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/`, `docs/IMPLEMENTATION-STATUS.md` §AI

---

## 4. AI Assistant — Estado Crítico

**Status: MOCK**

### Problema central:
`SendAssistantMessage` retorna respostas hardcoded — sem integração com LLM real.

### Frontend:
`AiAssistantPage.tsx` usa `mockConversations` hardcoded com comentário: *"AssistantPanel tem fallback mock quando backend falha"*

### Integração LLM:
- Ollama: configurado (`localhost:11434`, `qwen3.5:9b`), enabled=true
- OpenAI: configurado mas `enabled=false` por padrão
- `IExternalAIRoutingPort` existe como abstração
- `ExternalAI` module: 8 features marcadas como TODO stub

**Gap:** A infraestrutura de configuração de providers está correta. O routing de provider existe como abstração. Mas o handler `SendAssistantMessage` não invoca nenhum provider real — retorna hardcoded.

**Evidência:** `docs/CORE-FLOW-GAPS.md` §Fluxo 4, `docs/REBASELINE.md` §AI Knowledge

---

## 5. Grounding e Knowledge Retrieval

**Status: PARTIAL**

### Infraestrutura presente:
- Context builders e knowledge surfaces existem
- `POST /ai/context/enrich` existe mas sem retrieval real de dados
- Incidents e Runbooks têm persistência real via EfIncidentStore (usáveis como fontes)
- Contratos e mudanças têm dados reais (Catalog e ChangeGovernance)

### O que falta:
- Enriquecimento de contexto via queries reais dos módulos (IContractsModule, IChangeIntelligenceModule) — ambas são PLAN
- Model selection retorna modelos fictícios (`NexTrace-Internal-v1`) — sem conexão ao Model Registry real
- Grounding validado end-to-end: não existe

**Evidência:** `docs/CORE-FLOW-GAPS.md` §Fluxo 4

---

## 6. AI Agents

**Status: PARTIAL (UI presente, backend parcial)**

- `AiAgentsPage.tsx` e `AgentDetailPage.tsx` existem no frontend
- Conectados ao backend via API real
- `ADR-006-agent-runtime-foundation.md` documenta a arquitetura de agentes
- `IAiOrchestrationModule` = PLAN (sem implementação)

---

## 7. IDE Extensions Management

**Status: PLACEHOLDER**

- `IdeIntegrationsPage.tsx` existe no frontend
- Conectada ao backend
- Sem evidence de integração real com IDEs (VS Code, JetBrains)

---

## 8. Regras de Governança de IA — Verificação

| Regra | Estado | Evidência |
|---|---|---|
| IA com contexto do produto | Parcial — estrutura existe, não funcional | `context/enrich` sem retrieval real |
| IA com política por utilizador | Sim — AiGovernanceDbContext | Access policies implementadas |
| IA com autorização | Sim — RequirePermission nos endpoints | Endpoints protegidos |
| IA com auditabilidade | Parcial — AI Audit existe; assistant não persiste conversas reais | AiAuditPage conectada |
| IA com tenant awareness | Sim — tenant isolation no DbContext | RLS aplicado |
| IA com environment awareness | Parcial — estrutura existe | Não validado |
| IA com persona awareness | Parcial — prompts por persona documentados | Eficácia não validada |
| Controle de dados para modelos externos | Sim — OpenAI disabled por padrão | appsettings.json |
| Token budget governance | Sim — AiGovernanceDbContext | Implementado |
| Sem chat genérico sem contexto | VIOLADO — assistant retorna hardcoded | `SendAssistantMessage` |

---

## 9. Cross-Module AI Interfaces — Estado

| Interface | Estado | Impacto para IA |
|---|---|---|
| `IAiOrchestrationModule` | PLAN (empty) | Sem orquestração real de AI flows |
| `IExternalAiModule` | PLAN (empty) | Sem integração com providers externos |
| `IContractsModule` | PLAN | IA não pode consultar contratos dinamicamente |
| `IChangeIntelligenceModule` | PLAN | IA não pode consultar mudanças dinamicamente |

---

## 10. Risco Principal

**A governança existe mas o assistant que ela governa não funciona.**

Existe infrastructure completa de AI governance (políticas, budgets, model registry, audit). Mas o único componente que usa esta infraestrutura — o AI Assistant — retorna respostas hardcoded. Resultado: governança governa nada em produção.

---

## 11. Recomendações

| Ação | Prioridade | Impacto |
|---|---|---|
| Conectar `SendAssistantMessage` ao provider Ollama via `IExternalAIRoutingPort` | Crítica | Fluxo AI end-to-end |
| Implementar enriquecimento de contexto com dados reais dos módulos | Alta | Grounding útil |
| Conectar `AiAssistantPage` à API de conversas real | Alta | Frontend funcional |
| Implementar `IAiOrchestrationModule` com métodos reais | Alta | Orquestração |
| Gerar migrações para AiOrchestrationDbContext e ExternalAiDbContext | Média | Persistência deployável |
| Implementar os 8 ExternalAI handlers TODO | Média | Integração IA externa |
| Conectar Model Registry ao routing de providers | Média | Modelo selecionado por política |
| Validar que AI Audit persiste conversas reais | Alta | Auditabilidade real |
