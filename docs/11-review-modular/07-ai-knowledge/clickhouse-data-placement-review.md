# PARTE 7 — ClickHouse Data Placement Review

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Dados que devem ficar no PostgreSQL

| Dados | Tabela | Motivo |
|-------|--------|--------|
| Definição de modelos LLM | `aik_models` | Configuração transacional, low volume, precisa de FK e consistência |
| Configuração de providers | `aik_providers` | Configuração com credenciais, low volume |
| Definição de agents | `aik_agents` | Entidade rica com lógica de domínio |
| Políticas de acesso | `aik_access_policies` | Segurança — precisa de consistência forte |
| Orçamentos | `aik_budgets` | Controlo financeiro — precisa de transações |
| Quotas de tokens | `aik_token_quota_policies` | Controlo — precisa de consistência |
| Metadados de conversas | `aik_conversations` | Dados de utilizador, soft-delete, low volume |
| Mensagens de chat | `aik_messages` | Dados sensíveis, auditáveis, FK com conversa |
| Sources de conhecimento | `aik_sources` | Configuração, low volume |
| Estratégias de routing | `aik_routing_strategies` | Configuração, low volume |
| Providers externos | `aik_external_providers` | Configuração com credenciais |
| Políticas externas | `aik_external_policies` | Segurança |
| Knowledge captures | `aik_knowledge_captures` | Dados governados com workflow de aprovação |
| IDE clients | `aik_ide_clients` | Configuração (futuro) |
| Contextos de orquestração | `aik_orchestration_contexts` | Dados transacionais com FK |
| Artefactos de teste | `aik_test_artifacts` | Dados com workflow de review |
| Artefactos de agents | `aik_agent_artifacts` | Dados com workflow de review |

---

## 2. Dados que devem ir para ClickHouse

| Dados | Tabela PostgreSQL (origem) | Tabela ClickHouse (destino) | Volume | Motivo |
|-------|---------------------------|----------------------------|--------|--------|
| Registo de uso de tokens | `aik_token_usage_ledger` | `aik_ch_token_usage` | 🔴 Alto | Séries temporais, agregações por utilizador/modelo/tenant/dia |
| Entradas de uso geral | `aik_usage_entries` | `aik_ch_usage_entries` | 🔴 Alto | Séries temporais de uso por modelo e resultado |
| Decisões de routing | `aik_routing_decisions` | `aik_ch_routing_decisions` | 🟠 Médio-alto | Analytics de seleção de modelo, confidence tracking |
| Resultados de enriquecimento | `aik_enrichment_results` | `aik_ch_enrichment_results` | 🟠 Médio | Analytics de grounding e sources utilizadas |
| Inferências externas | `aik_external_inferences` | `aik_ch_external_inferences` | 🟠 Médio | Custo, latência, provider performance |
| Execuções de agents | `aik_agent_executions` | `aik_ch_agent_executions` | 🟠 Médio | Analytics de uso de agents, success rate, tokens |

---

## 3. Dados que NÃO devem ir para ClickHouse

| Dados | Motivo |
|-------|--------|
| Conteúdo de mensagens de chat | Dados sensíveis, PII, precisam de soft-delete e encriptação |
| Credenciais de providers | Segurança |
| Políticas de acesso | Dados de segurança, low volume |
| Definições de agents (system prompts) | Dados sensíveis e proprietários |
| Knowledge captures com dados aprovados | Dados governados |
| IDE client registrations | Dados de configuração |

---

## 4. Eventos mínimos de uso de IA

| Evento | Dimensões | Métricas | Origem |
|--------|-----------|----------|--------|
| `ai.chat.message_sent` | UserId, TenantId, ModelId, ConversationId | TokensUsed, ResponseTimeMs | SendAssistantMessage |
| `ai.chat.message_received` | UserId, TenantId, ModelId | TokensUsed, ResponseTimeMs | SendAssistantMessage (response) |
| `ai.agent.execution_started` | UserId, TenantId, AgentId, ModelId | EstimatedTokens | ExecuteAgent |
| `ai.agent.execution_completed` | UserId, TenantId, AgentId, ModelId, Status | TokensUsed, DurationMs | ExecuteAgent (result) |
| `ai.model.activated` | TenantId, ModelId, ProviderId | — | ActivateModel |
| `ai.model.deactivated` | TenantId, ModelId | — | UpdateModel |
| `ai.external.query_executed` | UserId, TenantId, ProviderId | TokensUsed, Cost, ResponseTimeMs | QueryExternalAI |
| `ai.routing.decision_made` | TenantId, ModelId, RoutingPath, Confidence | — | AIRoutingDecision |
| `ai.budget.threshold_reached` | TenantId, BudgetId, Period | CurrentUsage, Threshold | Token usage check |
| `ai.knowledge.capture_created` | UserId, TenantId, SourceType | — | CaptureExternalAIResponse |

---

## 5. Dimensões analíticas mínimas

| Dimensão | Tipo | Valores |
|----------|------|---------|
| `tenant_id` | UUID | ID do tenant |
| `user_id` | UUID | ID do utilizador |
| `model_id` | UUID | ID do modelo usado |
| `provider_id` | UUID | ID do provider |
| `agent_id` | UUID | ID do agent |
| `model_type` | Enum | ChatCompletion, Embedding, etc. |
| `routing_path` | Enum | Internal, External, Hybrid |
| `execution_status` | Enum | Success, Failed, TimedOut, etc. |
| `usage_result` | Enum | Success, PartialSuccess, Failed, Fallback |
| `timestamp` | DateTime | Data/hora do evento |
| `environment_id` | UUID (opcional) | Ambiente |

---

## 6. Métricas mínimas obrigatórias

| Métrica | Agregação | Dimensões |
|---------|-----------|-----------|
| Total de tokens usados | SUM por período | Tenant, User, Model |
| Custo estimado de IA | SUM por período | Tenant, Model, Provider |
| Número de mensagens de chat | COUNT por período | Tenant, User |
| Número de execuções de agents | COUNT por período | Tenant, Agent |
| Taxa de sucesso de agents | AVG(status=Success) | Agent, Tenant |
| Latência média de resposta | AVG(response_time_ms) | Model, Provider |
| Consultas externas realizadas | COUNT por período | Tenant, Provider |
| Utilização de orçamento (%) | CurrentUsage / BudgetLimit | Tenant, Period |

---

## 7. Agregações e séries temporais relevantes

| Agregação | Granularidade | Retenção |
|-----------|--------------|----------|
| Token usage por hora | Hourly | 90 dias |
| Token usage por dia | Daily | 1 ano |
| Token usage por mês | Monthly | 3 anos |
| Execuções de agents por dia | Daily | 1 ano |
| Custo por modelo por mês | Monthly | 3 anos |
| Latência P50/P95/P99 por modelo | Hourly | 30 dias |
| Top models por uso | Daily | 1 ano |
| Top agents por execução | Daily | 1 ano |

---

## 8. Chaves de correlação com PostgreSQL

| Chave ClickHouse | Tabela PostgreSQL | Utilização |
|-----------------|-------------------|------------|
| `model_id` | `aik_models.Id` | JOIN para nome/tipo do modelo |
| `provider_id` | `aik_providers.Id` | JOIN para nome do provider |
| `agent_id` | `aik_agents.Id` | JOIN para nome/categoria do agent |
| `user_id` | `iam_users.Id` | JOIN para nome do utilizador |
| `tenant_id` | `iam_tenants.Id` | JOIN para nome do tenant |

---

## 9. Nível de necessidade do ClickHouse

### **RECOMMENDED**

---

## 10. Justificação

| Critério | Avaliação |
|----------|-----------|
| Volume de dados | 🟠 Médio-alto — token usage logs crescem com uso real |
| Necessidade de séries temporais | ✅ Sim — trends de uso, custo, latência |
| Necessidade de agregações complexas | ✅ Sim — KPIs por tenant/user/model/period |
| PostgreSQL aguenta no MVP? | ✅ Sim — com índices adequados, PostgreSQL aguenta fase inicial |
| ClickHouse traz valor imediato? | ⚠️ Parcial — valor claro para dashboards de uso de IA, mas pode esperar |
| Risco de não ter ClickHouse | 🟡 Médio — sem ClickHouse, dashboards analíticos serão lentos em produção |

**Conclusão:** O módulo AI & Knowledge beneficia **significativamente** de ClickHouse para analytics de uso de IA, mas pode começar apenas com PostgreSQL no MVP. A migração de dados de log para ClickHouse deve ser preparada desde já (schema de eventos, dual-write pattern) mas implementada quando o volume justificar.

**Diferença vs Product Analytics:** Product Analytics foi classificado como REQUIRED porque é puramente analítico. AI & Knowledge é **RECOMMENDED** porque o core funcional (chat, agents, policies) vive inteiramente em PostgreSQL, e apenas os logs/métricas beneficiam de ClickHouse.
